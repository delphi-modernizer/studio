# Analyzers

Every project builds with the .NET analyzers at the latest level, SonarAnalyzer.CSharp and the engine's KH layout
rules, all as errors. These are the exceptions, each with its reason.

| Rule | Where | Why |
| --- | --- | --- |
| CS0612, CS0618 | WinUI libraries and the App | The XAML type generator references a property the Windows App SDK deprecated |
| CA1416 | App | Platform checks are the Windows App SDK's own; every API used exists on the minimum version |
| CS8305 | App | The TitleBar control is marked experimental in this SDK line |
| S2326 | `IRequest<TResponse>` | The type parameter is the response type `ISender.Send` infers |
