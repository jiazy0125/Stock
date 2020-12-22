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
		public ObservableCollection<CustomerHelper> Customers { get; } = new ObservableCollection<CustomerHelper>();

		public ObservableCollection<StockInfoHelper> Stocks { get; private set; } = new ObservableCollection<StockInfoHelper>();

		private readonly Dictionary<string, List<StockInfoHelper>> customersInfo = new Dictionary<string, List<StockInfoHelper>>();

		private string root = AppDomain.CurrentDomain.BaseDirectory;

		public DelegateCommand Calculate { get; private set; }
		public DelegateCommand Refresh { get; private set; }
		public DelegateCommand Save { get; private set; }
		public DelegateCommand CustomerAdd { get; private set; }
		public DelegateCommand CustomerDel { get; private set; }
		public DelegateCommand StockAdd { get; private set; }
		public DelegateCommand StockDel { get; private set; }
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

		private float totalBase = 1000;
		public float TotalBase
		{
			get => totalBase;
			set => SetProperty(ref totalBase, value);
		}

		private float totalExponet = 0;
		public float TotalExponent
		{
			get => totalExponet;
			set => SetProperty(ref totalExponet, value);
		}

		private string newCustomer="";
		public string NewCustomer
		{
			get => newCustomer;
			set => SetProperty(ref newCustomer, value);
		}
		#endregion
		public void CustomChanged(object sender, EventArgs e)
		{
			Stocks.Clear();
			if (CustomerSelected == null) return;
			Stocks.AddRange(customersInfo[customerSelected.Customer]);
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
			Customers.Clear();
			customersInfo.Clear();
			string infoFolder = Path.Combine(root, "CustomerInfo");

			try
			{
				if (!Directory.Exists(infoFolder))
				{
					Directory.CreateDirectory(infoFolder);
				}
				var data = ReadTxt(Path.Combine(root, "CustomerInfo\\Customers.txt"));
				foreach (string str in data)
				{ 
					var info=  Regex.Split(str, " ", RegexOptions.IgnoreCase);
					CustomerHelper ch = new CustomerHelper() { Customer = info[0], Exbase = float.Parse(info[1]), Weight = float.Parse(info[2]) };
					Customers.Add(ch);
					var content = ReadTxt(Path.Combine(root, $"CustomerInfo\\{info[0]}.txt"));
					List<StockInfoHelper> lt = new List<StockInfoHelper>();
					foreach (string txt in content)
					{
						string[] stockInfo = Regex.Split(txt.Trim(), " ", RegexOptions.IgnoreCase);
						StockInfoHelper si = new StockInfoHelper()
						{
							StockName = stockInfo[0],
							StockCode = stockInfo[1],
							ClosingPrice = float.Parse(stockInfo[2]),
							RecommendDay = stockInfo[3]
						};
						lt.Add(si);
					}

					customersInfo.Add(info[0], lt);
				}
			}
			catch { }
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

		public void WriteTxt(string path, string content)
		{
			FileStream fs;
			if (!File.Exists(path))
				fs = new FileStream(path, FileMode.Create);
			else fs = new FileStream(path, FileMode.Truncate);
			StreamWriter sw = new StreamWriter(fs);
			//开始写入
			sw.Write(content);
			//清空缓冲区
			sw.Flush();
			//关闭流
			sw.Close();
			fs.Close();
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
				float perWeightStock = 1;
				if (kvp.Value.Count > 0) perWeightStock = (float)1 / (float)kvp.Value.Count;
				float total = 1;
				foreach (StockInfoHelper si in kvp.Value)
				{

					float profit = (float.Parse(GetStcokInfo(si.StockCode)[3]) - si.ClosingPrice) / si.ClosingPrice;

					total += perWeightStock * profit;

				}

				var ct = Customers.First(t => t.Customer == kvp.Key);
				ct.Profit = total;
				ct.Exponent = ct.Exbase * total;


			}
			float totalPer = 0;
			foreach (CustomerHelper ct in Customers)
			{
				totalPer += ct.Profit * ct.Weight;
			}
			TotalExponent = totalBase * totalPer;
		
		}

		private void SaveCustomers()
		{
			string content = "";
			foreach (CustomerHelper ct in Customers)
			{
				content += $"{ct.Customer} {ct.Exbase} {ct.Weight}\n";
			}

			WriteTxt(Path.Combine(root, "CustomerInfo\\Customers.txt"), content.Trim());
		}

		private void SaveStocks()
		{
			foreach (KeyValuePair<string, List<StockInfoHelper>> kvp in customersInfo)
			{
				string contentStock = "";
				foreach (StockInfoHelper si in kvp.Value)
				{
					contentStock += $"{si.StockName} {si.StockCode} {si.ClosingPrice} {si.RecommendDay}\n";
				}
				WriteTxt(Path.Combine(root, $"CustomerInfo\\{kvp.Key}.txt"), contentStock);
			}
		}

		public MainWindowViewModel()
		{
			LoadAllInfo();
			Calculate = new DelegateCommand(() => { CalculateExponent(); });
			Refresh = new DelegateCommand(() => { LoadAllInfo(); });
			Save = new DelegateCommand(() =>
			{
				SaveCustomers();
				SaveStocks();
			
			});

			CustomerAdd = new DelegateCommand(() =>
			{
				if (newCustomer.Length <= 0)
				{
					MessageBox.Show("客户名称不得为空");
					return;
				}
				CustomerHelper ch = new CustomerHelper() { Customer = newCustomer, Exbase = 1000, Weight = 0 };
				Customers.Add(ch);
				customersInfo.Add(newCustomer, new List<StockInfoHelper>());
			
			});

			CustomerDel = new DelegateCommand(() => 
			{
				if (customerSelected == null)
				{
					MessageBox.Show("请先选择需要删除的用户");
					return;
				}

				MessageBoxResult dr = MessageBox.Show("该操作将删除永久删除用户所有信息","提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
				if (!(dr == MessageBoxResult.OK)) return;
				try
				{
					CustomerHelper ch = Customers.First(t => t == CustomerSelected);
					Customers.Remove(ch);
					customersInfo.Remove(ch.Customer);

					File.Delete(Path.Combine(root, $"CustomerInfo\\{ch.Customer}.txt"));
					SaveCustomers();
				}
				catch { }
			});

			StockAdd = new DelegateCommand(() => 
			{
				if (customerSelected == null)
				{
					MessageBox.Show("请先选中一个客户");
					return;
				}
				try
				{
					string st= $"S{DateTime.Now:ffffff}";
					StockInfoHelper si = new StockInfoHelper() { StockName = st, StockCode = "sh600000", ClosingPrice = 0, RecommendDay = "1901-01-01" };
					Stocks.Add(si);
					customersInfo[customerSelected.Customer].Add(si);
				}
				catch { }
			});

			StockDel = new DelegateCommand(() => 
			{

				if (stockSelected == null)
				{
					MessageBox.Show("请先选中一支股票");
					return;
				}
				MessageBoxResult dr = MessageBox.Show("该操作将删除永久删除股票信息", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
				if (!(dr == MessageBoxResult.OK)) return;
				try
				{
					customersInfo[customerSelected.Customer].Remove(stockSelected);
					Stocks.Remove(stockSelected);
					SaveStocks();
				}
				catch { }
			
			});


		}
	}

	public class StockInfoHelper:BindableBase
	{

		private string stockName;
		private string stockCode;
		private float closingPrice = 0;
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
		public float ClosingPrice
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
		private float exponent = 0;
		private float exbase = 0;
		private float weight = 0;


		public string Customer
		{
			get => customer;
			set => SetProperty(ref customer, value);
		}
		public float Exponent
		{
			get => exponent;
			set => SetProperty(ref exponent, value);
		}

		public float Exbase
		{
			get => exbase;
			set => SetProperty(ref exbase, value);
		}

		public float Weight
		{
			get => weight;
			set => SetProperty(ref weight, value);
		}

		public float Profit { get; set; }


	}
}
