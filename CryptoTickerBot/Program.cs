﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Exchanges;
using CryptoTickerBot.Helpers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using static System.Console;
using Timer = System.Timers.Timer;

// ReSharper disable FunctionNeverReturns
#pragma warning disable 4014

namespace CryptoTickerBot
{
	public class Program
	{
		private static readonly string[] Scopes = {SheetsService.Scope.Spreadsheets};
		private const string ApplicationName = "Crypto Ticker Bot";
		private const string SheetName = "Tickers";
		private static string sheetId;
		private static SheetsService service;
		private static readonly object SheetUpdateLock = new object ( );

		private static readonly Dictionary<CryptoExchange, string> SheetsRanges = new Dictionary<CryptoExchange, string>
		{
			[CryptoExchange.BitBay] = "A3:D6",
			[CryptoExchange.Koinex] = "A12:D15",
			[CryptoExchange.Binance] = "A20:D23",
			[CryptoExchange.CoinDelta] = "A29:D32",
			[CryptoExchange.Coinbase] = "A37:D40",
		};

		private static readonly HashSet<CryptoExchange> PendingUpdates = new HashSet<CryptoExchange> ( );

		public static void Main ( string[] args )
		{
			FiatConverter.StartMonitor ( );

			var exchanges = new Dictionary<CryptoExchange, CryptoExchangeBase>
			{
				[CryptoExchange.Koinex] = new KoinexExchange ( ),
				[CryptoExchange.BitBay] = new BitBayExchange ( ),
				[CryptoExchange.Binance] = new BinanceExchange ( ),
				[CryptoExchange.CoinDelta] = new CoinDeltaExchange ( ),
				[CryptoExchange.Coinbase] = new CoinbaseExchange ( ),
			};

			foreach ( var exchange in exchanges.Values )
			{
				exchange.Changed += ( e, coin ) =>
				{
					lock ( SheetUpdateLock )
						PendingUpdates.Add ( e.Id );
				};
				exchange.Changed += ( e, coin ) => WriteLine ( $"{e.Name,-10} {e[coin.Symbol]}" );
				exchange.GetExchangeData ( CancellationToken.None );
			}

			sheetId = File.ReadAllText ( "sheet.id" );
			CreateSheetsService ( );

			StartAutoSheetsUpdater ( exchanges );

			while ( true )
				Thread.Sleep ( 1 );
		}

		private static void StartAutoSheetsUpdater ( IReadOnlyDictionary<CryptoExchange, CryptoExchangeBase> exchanges )
		{
			Task.Run ( ( ) =>
			{
				Thread.Sleep ( 10000 );
				var updateTimer = new Timer ( 1000 );
				updateTimer.Elapsed += ( sender, eventArgs ) =>
				{
					lock ( SheetUpdateLock )
					{
						foreach ( var id in PendingUpdates )
						{
							var range = SheetsRanges[id];
							UpdateSheet ( range, exchanges[id] );
							WriteLine ( $"Updated Sheets for {id}" );
						}
						PendingUpdates.Clear ( );
					}
				};
				updateTimer.Start ( );
			} );
		}

		private static void CreateSheetsService ( )
		{
			var credential = GetCredentials ( );

			service = new SheetsService ( new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName
			} );
		}

		private static void UpdateSheet (
			string range,
			CryptoExchangeBase exchange
		)
		{
			var valueRange = new ValueRange
			{
				Values = exchange.ToSheetRows ( ),
				Range = $"{SheetName}!{range}"
			};
			var requestBody = new BatchUpdateValuesRequest
			{
				ValueInputOption = "USER_ENTERED",
				Data = new List<ValueRange> {valueRange}
			};

			var request = service.Spreadsheets.Values.BatchUpdate ( requestBody, sheetId );

			request.ExecuteAsync ( );
		}

		private static UserCredential GetCredentials ( )
		{
			UserCredential credential;

			using ( var stream =
				new FileStream ( "client_secret.json", FileMode.Open, FileAccess.Read ) )
			{
				var credPath = Environment.GetFolderPath (
					Environment.SpecialFolder.Personal );
				credPath = Path.Combine ( credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json" );

				credential = GoogleWebAuthorizationBroker.AuthorizeAsync (
					GoogleClientSecrets.Load ( stream ).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore ( credPath, true ) ).Result;
				WriteLine ( "Credential file saved to: " + credPath );
			}

			return credential;
		}
	}
}