namespace PupilTrack;

public partial class OneLegDetailsPage : ContentPage
{
	public OneLegDetailsPage()
	{
		InitializeComponent();
	}

    private async void OneLegDetailsBackClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HomePage());
    }
}