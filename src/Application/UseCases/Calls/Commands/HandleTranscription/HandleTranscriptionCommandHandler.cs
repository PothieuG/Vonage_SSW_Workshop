using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Text;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;

internal sealed class HandleTranscriptionCommandHandler(IVonageService vonageService, IMcpService mcpService)
    : IRequestHandler<HandleTranscriptionCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(HandleTranscriptionCommand request, CancellationToken cancellationToken)
    {
        var transcriptionDetails = await GetTranscriptionDetails(request, cancellationToken);
        var call = await vonageService.GetCallInfoByConversationUuidAsync(request.Request.ConversationUuid, cancellationToken);
        var smsResult = await MergeCallAndTranscription(transcriptionDetails, call)
            .ThenAsync(smsDetails => vonageService.SendSmsAsync(smsDetails.Call, smsDetails.Transcript.RawTranscript, smsDetails.Transcript.SummarizedTranscript, cancellationToken));
        return smsResult.IsError ? smsResult.Errors : Result.Success;
    }

    private static ErrorOr<SmsInfo> MergeCallAndTranscription(ErrorOr<TranscriptionDetails> transcriptionDetails, ErrorOr<CallInfo> call) =>
        transcriptionDetails .Merge<TranscriptionDetails, CallInfo, SmsInfo>(call, (transcription, callInformation) =>  new SmsInfo(callInformation, transcription));

    private async Task<ErrorOr<TranscriptionDetails>> GetTranscriptionDetails(HandleTranscriptionCommand request, CancellationToken cancellationToken)
    {
        var downloadedTranscription = await vonageService.DownloadTranscriptionAsync(
            request.Request.TranscriptionUrl,
            cancellationToken);
        var summarizedTranscript = await downloadedTranscription.ThenAsync(transcript => mcpService.ProcessTranscriptWithMcpAsync(transcript, cancellationToken));
        return downloadedTranscription.Merge<string, string, TranscriptionDetails>(summarizedTranscript, (t, s) => new TranscriptionDetails(t, s));
    }

    internal sealed class HandleTranscriptionCommandValidator : AbstractValidator<HandleTranscriptionCommand>
    {
        public HandleTranscriptionCommandValidator()
        {
            RuleFor(x => x.Request.TranscriptionUrl)
                .NotEmpty()
                .WithMessage("L'URL de transcription est requise.");
        }
    }
}

public record TranscriptionDetails(string RawTranscript, string SummarizedTranscript);
public record SmsInfo(CallInfo Call, TranscriptionDetails Transcript);

public static class ErrorOrExtensions
{
    public static ErrorOr<TDestination> Merge<TSource1, TSource2, TDestination>(this ErrorOr<TSource1> source1,
        ErrorOr<TSource2> source2, Func<TSource1, TSource2, ErrorOr<TDestination>> merge) =>
        !source1.IsError && !source2.IsError
            ? merge(source1.Value, source2.Value)
            : FetchError<TSource1, TSource2, TDestination>(source1, source2);

    private static ErrorOr<TDestination> FetchError<TSource1, TSource2, TDestination>(this ErrorOr<TSource1> source1,
        ErrorOr<TSource2> source2) =>
        source1.IsError ? source1.FirstError : source2.FirstError;
}