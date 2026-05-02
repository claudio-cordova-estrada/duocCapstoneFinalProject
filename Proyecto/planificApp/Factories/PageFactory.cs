using System;
using planificApp.Data;
using planificApp.ViewModels;

namespace planificApp.Factories;

public class PageFactory(Func<ApplicationPageNames, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPageNames pageName) => factory.Invoke(pageName);
}