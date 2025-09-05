namespace COGLyricsScanner.Views
{

public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void OnSendFeedbackClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await DisplayActionSheet(
                "Send Feedback", 
                "Cancel", 
                null, 
                "Email Support", 
                "Report Bug", 
                "Feature Request");

            switch (result)
            {
                case "Email Support":
                    await SendEmail("Support Request", "I need help with...");
                    break;
                case "Report Bug":
                    await SendEmail("Bug Report", "I found a bug:\n\nSteps to reproduce:\n1. \n2. \n3. \n\nExpected behavior:\n\nActual behavior:\n\nDevice information:\n- Platform: \n- Version: ");
                    break;
                case "Feature Request":
                    await SendEmail("Feature Request", "I would like to request a new feature:\n\nDescription:\n\nUse case:\n\nBenefit:");
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to send feedback: {ex.Message}", "OK");
        }
    }

    private async Task SendEmail(string subject, string body)
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = $"COG Lyrics Scanner - {subject}",
                Body = body,
                To = new List<string> { "support@coglyricsscanner.com" }
            };

            await Email.Default.ComposeAsync(message);
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Not Supported", "Email is not supported on this device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open email client: {ex.Message}", "OK");
        }
    }
}
}