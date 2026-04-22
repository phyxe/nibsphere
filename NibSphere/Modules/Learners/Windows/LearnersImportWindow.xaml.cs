using System.Windows;

namespace NibSphere.Modules.Learners.Windows
{
	public partial class LearnersImportWindow : Window
	{
		public LearnersImportWindow()
		{
			InitializeComponent();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}