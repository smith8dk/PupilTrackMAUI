namespace PupilTrack;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
	}

    private async void AboutBackClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HomePage());
    }
}