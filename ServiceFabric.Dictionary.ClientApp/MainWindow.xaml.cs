using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ServiceFabric.Dictionary.DictionaryService;
using ServiceFabric.Dictionary.IndexService;

namespace ServiceFabric.Dictionary.ClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class MainWindow : Window
    {
        private readonly IServiceProxyFactory _serviceProxyFactory;
        private static readonly Uri DictionaryServiceUri = new Uri("fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.DictionaryService");
        private static readonly Uri IndexServiceUri = new Uri("fabric:/ServiceFabric.Dictionary/ServiceFabric.Dictionary.IndexService");

        public MainWindow()
            : this (new ServiceProxyFactory())
        {
        }

        public MainWindow(IServiceProxyFactory serviceProxyFactory)
        {
            _serviceProxyFactory = serviceProxyFactory;
            InitializeComponent();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string meaning = InputMeaning.Text;
            string word = InputWord.Text;

            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(meaning))
                return;

            AddButton.IsEnabled = false;
            Status.Content = "Adding word";
            bool ok = await Try(async () =>
            {
                var proxy = _serviceProxyFactory.CreateServiceProxy<IIndexService>(IndexServiceUri);
                await proxy.Add(word, meaning).ConfigureAwait(false);
            });
            if (ok) Status.Content = "Word added";
            AddButton.IsEnabled = true;

            InputMeaning.Clear();
            InputWord.Clear();

        }

        private async void LookupButton_Click(object sender, RoutedEventArgs e)
        {
            string word = InputWord.Text;
            if (string.IsNullOrWhiteSpace(word))
                return;

            InputMeaning.Clear();
            LookupButton.IsEnabled = false;
            InputMeaning.Text = await Try(async () =>
            {
                var proxy = _serviceProxyFactory.CreateServiceProxy<IIndexService>(IndexServiceUri, ServicePartitionKey.Singleton, TargetReplicaSelector.RandomInstance);
               var meaning = await proxy.Lookup(word).ConfigureAwait(false);
                return meaning;
            });
            LookupButton.IsEnabled = true;
        }

        private async void LookupButtonDictSvPartOne_Click(object sender, RoutedEventArgs e)
        {
            string word = InputWord.Text;
            if (string.IsNullOrWhiteSpace(word))
                return;

            InputMeaning.Clear();
            LookupButtonDictSvPartOne.IsEnabled = false;
            InputMeaning.Text = await Try(async () =>
            {
                var proxy = _serviceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(1L), TargetReplicaSelector.PrimaryReplica, DictionaryServiceListenerSettings.RemotingListenerName);
                return await proxy.Lookup(word).ConfigureAwait(false);
            });
            LookupButtonDictSvPartOne.IsEnabled = true;
        }

        private async void LookupButtonDictSvPartTwo_Click(object sender, RoutedEventArgs e)
        {
            string word = InputWord.Text;
            if (string.IsNullOrWhiteSpace(word))
                return;

            InputMeaning.Clear();
            LookupButtonDictSvPartTwo.IsEnabled = false;
            InputMeaning.Text = await Try(async () =>
            {
                var proxy = _serviceProxyFactory.CreateServiceProxy<IDictionaryService>(DictionaryServiceUri, new ServicePartitionKey(-1L), TargetReplicaSelector.PrimaryReplica, DictionaryServiceListenerSettings.RemotingListenerName);
                return await proxy.Lookup(word).ConfigureAwait(false);
            });
            LookupButtonDictSvPartTwo.IsEnabled = true;

        }

        private async Task<bool> Try(Func<Task> work)
        {
            bool ok = false;
            try
            {
                await Task.Run(work).ConfigureAwait(true);
                ok = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error processing work: {ex.Message}");
                Status.Content = $"Error: {ex.Message}";
            }
            return ok;
        }

        private async Task<string> Try(Func<Task<string>> work)
        {
            string result = null;
            try
            {
                result = await work().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error processing work: {ex.Message}");
                Status.Content = $"Error: {ex.Message}";
            }
            return result;
        }
    }
}
