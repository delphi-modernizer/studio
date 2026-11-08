using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DelphiModernizer.Studio.Application.Abstractions.Messaging;
using DelphiModernizer.Studio.Application.Abstractions.Workspaces;
using DelphiModernizer.Studio.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DelphiModernizer.Studio.Mediator.Tests;

public sealed class MediatorTests
{
    [Fact]
    public async Task A_request_reaches_its_handler_through_the_behaviors_in_registration_order()
    {
        var journal = new Journal();
        var sender = Services.Sender(new WorkspaceSession(), services => {
            services.AddSingleton(journal);
            services.AddMediator(typeof(MediatorTests).Assembly);
            services.AddTransient<IPipelineBehavior<Echo, string>, Outer>();
            services.AddTransient<IPipelineBehavior<Echo, string>, Inner>();
        });

        var answer = await sender.Send(new Echo("hello"), TestContext.Current.CancellationToken);

        Assert.Equal("hello", answer);
        Assert.Equal(["outer", "inner", "handler"], journal.Entries);
    }

    [Fact]
    public async Task A_notification_reaches_every_handler_once_even_when_the_assembly_is_added_twice()
    {
        var journal = new Journal();
        var services = new ServiceCollection();
        services.AddSingleton(journal);
        services.AddLogging();
        services.AddMediator(typeof(MediatorTests).Assembly);
        services.AddMediator(typeof(MediatorTests).Assembly);
        var publisher = services
            .BuildServiceProvider()
            .GetRequiredService<IPublisher>();

        await publisher.Publish(new Happened(), TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], journal.Entries);
    }

    [Fact]
    public async Task A_request_sent_after_the_page_scope_ended_still_reaches_its_handler()
    {
        var journal = new Journal();
        var services = new ServiceCollection();
        services.AddSingleton(journal);
        services.AddLogging();
        services.AddMediator(typeof(MediatorTests).Assembly);
        await using var provider = services.BuildServiceProvider();
        var page = provider.CreateScope();
        var sender = page.ServiceProvider.GetRequiredService<ISender>();

        page.Dispose();
        var answer = await sender.Send(new Echo("after"), TestContext.Current.CancellationToken);

        Assert.Equal("after", answer);
        Assert.Equal(["handler"], journal.Entries);
    }

    public sealed class Journal
    {
        public List<string> Entries { get; } = [];
    }

    public sealed record Echo(string Text) : IRequest<string>;

    public sealed record Happened : INotification;

    public sealed class EchoHandler(Journal journal) : IRequestHandler<Echo, string>
    {
        public Task<string> Handle(Echo request, CancellationToken cancellationToken)
        {
            journal.Entries.Add("handler");

            return Task.FromResult(request.Text);
        }
    }

    public sealed class Outer(Journal journal) : IPipelineBehavior<Echo, string>
    {
        public Task<string> Handle(
            Echo request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            journal.Entries.Add("outer");

            return next();
        }
    }

    public sealed class Inner(Journal journal) : IPipelineBehavior<Echo, string>
    {
        public Task<string> Handle(
            Echo request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            journal.Entries.Add("inner");

            return next();
        }
    }

    public sealed class FirstHandler(Journal journal) : INotificationHandler<Happened>
    {
        public Task Handle(Happened notification, CancellationToken cancellationToken)
        {
            journal.Entries.Add("first");

            return Task.CompletedTask;
        }
    }

    public sealed class SecondHandler(Journal journal) : INotificationHandler<Happened>
    {
        public Task Handle(Happened notification, CancellationToken cancellationToken)
        {
            journal.Entries.Add("second");

            return Task.CompletedTask;
        }
    }
}
