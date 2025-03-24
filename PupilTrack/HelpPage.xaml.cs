namespace PupilTrack;

public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    private void ToggleFAQ1(object sender, EventArgs e)
    {
        FAQ1Answer.IsVisible = !FAQ1Answer.IsVisible;
    }

    private void ToggleFAQ2(object sender, EventArgs e)
    {
        FAQ2Answer.IsVisible = !FAQ2Answer.IsVisible;
    }

    private void ToggleFAQ3(object sender, EventArgs e)
    {
        FAQ3Answer.IsVisible = !FAQ3Answer.IsVisible;
    }
}
