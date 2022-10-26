using System;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using Wpf.Ui.Mvvm.Contracts;

namespace Xabbo.Scripter.Services;

internal class PageService : IPageService
{
    private readonly IServiceProvider _serviceProvider;

    public PageService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T? GetPage<T>() where T : class => _serviceProvider.GetRequiredService<T>();

    public FrameworkElement? GetPage(Type pageType) => (FrameworkElement)_serviceProvider.GetRequiredService(pageType);
}
