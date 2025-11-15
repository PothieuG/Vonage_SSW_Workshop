using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Text;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;

internal sealed class HandleTranscriptionCommandHandler(IVonageService vonageService)
    : IRequestHandler<HandleTranscriptionCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(HandleTranscriptionCommand request, CancellationToken cancellationToken)
    {
        var downloadedTranscription = await vonageService.DownloadTranscriptionAsync(
            request.Request.TranscriptionUrl,
            cancellationToken);
        var call = await vonageService.GetCallInfoByConversationUuidAsync(request.Request.ConversationUuid, cancellationToken);
        var smsResult = await downloadedTranscription
            .Merge<string, CallInfo, SmsInfo>(call, (transcription, callInformation) => new SmsInfo(callInformation, transcription))
            .ThenAsync(sms => vonageService.SendSmsAsync(sms.Call, sms.Transcript, cancellationToken));
        return smsResult.IsError ? smsResult.Errors : Result.Success;
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

public record SmsInfo(CallInfo Call, string Transcript);

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