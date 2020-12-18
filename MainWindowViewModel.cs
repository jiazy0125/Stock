using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace StockWeight
{
	class MainWindowViewModel : BindableBase
	{
		private double perWeightCustomer = 0;
		private double perWeightStock = 0;
		public ObservableCollection<CustomerHelper> Customers { get; } = new ObservableCollection<CustomerHelper>();

		public ObservableCollection<StockInfoHelper> Stocks { get; private set; } = new ObservableCollection<StockInfoHelper>();

		private Dictionary<string, List<StockInfoHelper>> customersInfo = new Dictionary<string, List<StockInfoHelper>>();

		private string root = AppDomain.CurrentDomain.BaseDirectory;

		public DelegateCommand Calculate { get; private set; }

		#region propertis
		private CustomerHelper customerSelected;
		public CustomerHelper CustomerSelected
		{
			get => customerSelected;
			set => SetProperty(ref customerSelected, value);
		}

		private StockInfoHelper stockSelected;
		public StockInfoHelper StockSelected
		{
			get => stockSelected;
			set => SetProperty(ref stockSelected, value);
		}

		private string openToday;
		public string OpenToday
		{
			get => openToday;
			set => SetProperty(ref openToday, value);
		}
		private string colsing;
		public string Closing
		{
			get => colsing;
			set => SetProperty(ref colsing, value);
		}
		private string current;
		public string CurrentPrice
		{
			get => current;
			set => SetProperty(ref current, value);
		}
		private string highToday;
		public string HighPrice
		{
			get => highToday;
			set => SetProperty(ref highToday, value);
		}
		private string lowToday;
		public string LowPrice
		{
			get => lowToday;
			set => SetProperty(ref lowToday, value);
		}

		private double baseExponent = 1000;
		public double BaseExponent
		{
			get => baseExponent;
			set => SetProperty(ref baseExponent, value);
		}

		#endregion
		public void CustomChanged(object sender, EventArgs e)
		{
			Stocks.Clear();
			Stocks.AddRange(customersInfo[customerSelected.Customer]);
			perWeightStock = perWeightCustomer / Stocks.Count;
		}
		public void StockChanged(object sender, EventArgs e)
		{
			if (stockSelected != null)
			{				
				var details = GetStcokInfo(stockSelected.StockCode);
				OpenToday = details[1];
				Closing = details[2];
				CurrentPrice = details[3];
				HighPrice = details[4];
				LowPrice = details[5];
			}
		}

		private void LoadAllInfo()
		{
			string infoFolder = Path.Combine(root, "CustomerInfo");

			try
			{
				if (!Directory.Exists(infoFolder))
				{
					Directory.CreateDirectory(infoFolder);
				}

				var files = Directory.GetFiles(infoFolder, "*.txt");
				foreach (var file in files)
				{
					string fileName = Path.GetFileNameWithoutExtension(file);
					CustomerHelper ch = new CustomerHelper() { Customer = fileName };
					Customers.Add(ch);
					var content = ReadTxt(file);
					List<StockInfoHelper> lt = new List<StockInfoHelper>();
					foreach (string txt in content)
					{
						string[] stockInfo = Regex.Split(txt.Trim(), " ", RegexOptions.IgnoreCase);
						StockInfoHelper si = new StockInfoHelper()
						{
							StockName = stockInfo[0],
							StockCode = stockInfo[1],
							ClosingPrice = Convert.ToDouble(stockInfo[2]),
							RecommendDay = stockInfo[3]
						};
						lt.Add(si);
					}

					customersInfo.Add(fileName, lt);
				}
				perWeightCustomer = 1 / Customers.Count();
			}
			catch { perWeightCustomer = 0; }
		}

		public List<string> ReadTxt(string path)
		{
			List<string> txt = new List<string>();

			using (StreamReader sr = new StreamReader(path, Encoding.GetEncoding("utf-8")))
			{
				int lineCount = 0;
				while (sr.Peek() > 0)
				{
					lineCount++;
					string temp = sr.ReadLine();
					txt.Add(temp);
				}
			}

			return txt;
		}

		private string[] GetStcokInfo(string stockCode)
		{
			try
			{
				WebClient MyWebClient = new WebClient
				{
					Credentials = CredentialCache.DefaultCredentials//获取或设置用于向Internet资源的请求进行身份验证的网络凭据
				};
				byte[] pageData = MyWebClient.DownloadData($"http://hq.sinajs.cn/list={stockCode}"); //从指定网站下载数据
				string pageHtml = Encoding.Default.GetString(pageData);  //如果获取网站页面采用的是GB2312，则使用这句            
																		 //string pageHtml = Encoding.UTF8.GetString(pageData); //如果获取网站页面采用的是UTF-8，则使用这句
				Console.WriteLine(pageHtml);//在控制台输入获取的内容


				Regex pattern = new Regex("[\";]");
				string temp = pattern.Replace(pageHtml.Substring(pageHtml.IndexOf("=") + 1), "");

				
				string[] stockInfo = Regex.Split(temp, ",", RegexOptions.IgnoreCase);
				return stockInfo;

			}
			catch (Exception)
			{
				return null;
			}
		}

		private void CalculateExponent()
		{
			foreach (KeyValuePair<string, List<StockInfoHelper>> kvp in customersInfo)
			{
				perWeightStock = perWeightCustomer / kvp.Value.Count;
				foreach (StockInfoHelper si in kvp.Value)
				{ 
					
				
				
				}
			
			
			
			
			}
		
		}



		public MainWindowViewModel(IRegionManager regionManager, IEventAggregator ea, IContainerExtension container, IModuleCatalog mc)
		{
			LoadAllInfo();

		}
	}

	public class StockInfoHelper:BindableBase
	{

		private string stockName;
		private string stockCode;
		private double closingPrice;
		private string recommendDay;

		public string StockName
		{
			get => stockName;
			set => SetProperty(ref stockName, value);
		}
		public string StockCode
		{
			get => stockCode;
			set => SetProperty(ref stockCode, value);
		}
		public double ClosingPrice
		{
			get => closingPrice;
			set => SetProperty(ref closingPrice, value);
		}
		public string RecommendDay
		{
			get => recommendDay;
			set => SetProperty(ref recommendDay, value);
		}

	}

	public class CustomerHelper : BindableBase
	{

		private string customer;
		private double exponent;


		public string Customer
		{
			get => customer;
			set => SetProperty(ref customer, value);
		}
		public double Exponent
		{
			get => exponent;
			set => SetProperty(ref exponent, value);
		}


	}
}
