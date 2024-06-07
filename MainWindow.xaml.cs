using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Navigation;


namespace CertificateManagerWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadCertificates();

        }

        public class CertificateViewModel
        {
            public bool IsSelected { get; set; }
            public string CN { get; set; }
            public string Issuer { get; set; }
            public DateTime NotAfter { get; set; }
            public string NotAfterFormatted { get; set; }
            public bool IsExpired { get; set; }
            public X509Certificate2 Certificate { get; set; }
        }

        private void CertificatesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CertificatesDataGrid.SelectedItem is CertificateViewModel selectedCertificate)
            {
                X509Certificate2UI.DisplayCertificate(selectedCertificate.Certificate);
            }
        }

        private string GetCommonName(string subject)
        {
            var parts = subject.Split(',');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("CN="))
                {
                    return part.Trim().Substring(3);
                }
            }
            return subject;
        }

        private void LoadCertificates()
        {
            var certificates = new List<CertificateViewModel>();

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            foreach (X509Certificate2 cert in store.Certificates)
            {
                string cn = GetCommonName(cert.Subject);
                string cn2 = GetCommonName(cert.Issuer);
                certificates.Add(new CertificateViewModel
                {
                    CN = cn,
                    Issuer = cn2,
                    NotAfter = cert.NotAfter,
                    NotAfterFormatted = cert.NotAfter.ToString("dd.MM.yyyy"),
                    IsExpired = cert.NotAfter < DateTime.Now,
                    Certificate = cert
                });
            }

            store.Close();

            CertificatesDataGrid.ItemsSource = certificates.OrderBy(c => c.NotAfter).ToList();

        }

        private void ButtonDeleteExpired_Click(object sender, RoutedEventArgs e)
        {
            var certsToDelete = new List<X509Certificate2>();

            foreach (var item in CertificatesDataGrid.Items)
            {
                var cert = item as CertificateViewModel;
                if (cert != null && cert.IsExpired && cert.IsSelected)
                {
                    certsToDelete.Add(cert.Certificate);
                }
            }

            if (certsToDelete.Count == 0)
            {
                MessageBox.Show("Ни один сертификат не выбран!");
                return;
            }

            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранные сертификаты?", "Подтвердите операцию!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            foreach (var cert in certsToDelete)
            {
                store.Remove(cert);
            }

            store.Close();
            LoadCertificates();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonSelectExpired_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CertificatesDataGrid.Items)
            {
                var cert = item as CertificateViewModel;
                if (cert != null && cert.IsExpired)
                {
                    cert.IsSelected = true;
                }
            }

            CertificatesDataGrid.Items.Refresh();
        }

    }


}
