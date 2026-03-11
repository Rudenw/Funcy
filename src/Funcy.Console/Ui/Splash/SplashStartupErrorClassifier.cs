namespace Funcy.Console.Ui.Splash;

public static class SplashStartupErrorClassifier
{
    public static bool IsAzureNotLoggedInError(Exception exception)
    {
        var message = exception.ToString();
        return message.Contains("Please run 'az login'", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("az account show", StringComparison.OrdinalIgnoreCase);
    }
}
