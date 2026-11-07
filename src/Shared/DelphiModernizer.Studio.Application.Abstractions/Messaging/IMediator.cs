namespace DelphiModernizer.Studio.Application.Abstractions.Messaging;

/// <summary>Both halves of the in-process message bus.</summary>
public interface IMediator : ISender, IPublisher;
