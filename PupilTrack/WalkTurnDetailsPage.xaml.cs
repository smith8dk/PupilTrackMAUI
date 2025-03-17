namespace PupilTrack;

public partial class WalkTurnDetailsPage : ContentPage
{
	public WalkTurnDetailsPage()
	{
		InitializeComponent();
	}

    private async void WalkTurnDetailsBackClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HomePage());
    }
}