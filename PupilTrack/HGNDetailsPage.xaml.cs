namespace PupilTrack;

public partial class HGNDetailsPage : ContentPage
{
	public HGNDetailsPage()
	{
		InitializeComponent();
	}

	private async void HGNDetailsBackClicked(object sender, EventArgs e)
	{
    	await Navigation.PushAsync(new MainPage());
	}

	private async void HGNDetailsResultsClicked(object sender, EventArgs e)
	{
    	await Navigation.PushAsync(new HGNResultsPage());
	}

    private void Label_HandlerChanged(object sender, EventArgs e)
    {
    }
}