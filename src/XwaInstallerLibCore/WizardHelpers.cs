namespace XwaInstallerLibCore;

internal static class WizardHelpers
{
    public static WizardPage? GetPreviousPage(Wizard wizard, WizardPage page)
    {
        WizardPage? wizardPage = page.PreviousPage;

        if (wizardPage is null)
        {
            int num = wizard.Pages.IndexOf(page) - 1;

            if (num >= 0)
            {
                wizardPage = wizard.Pages[num];
            }
        }

        return wizardPage;
    }

    public static WizardPage? GetNextPage(Wizard wizard, WizardPage page)
    {
        WizardPage? wizardPage = page.NextPage;

        if (wizardPage is null)
        {
            int num = wizard.Pages.IndexOf(page) + 1;

            if (num < wizard.Pages.Count)
            {
                wizardPage = wizard.Pages[num];
            }
        }

        return wizardPage;
    }

    public static void RemovePage(Wizard wizard, WizardPage page)
    {
        WizardPage? previousPage = GetPreviousPage(wizard, page);
        WizardPage? nextPage = GetNextPage(wizard, page);

        if (previousPage is not null)
        {
            previousPage.NextPage = nextPage;
        }

        if (nextPage is not null)
        {
            nextPage.PreviousPage = previousPage;
        }

        if (wizard.CurrentPage == page)
        {
            wizard.CurrentPage = nextPage;
        }

        wizard.Pages.Remove(page);
    }
}
