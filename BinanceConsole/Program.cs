using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BinanceExchange.API;
using BinanceExchange.API.Client;
using BinanceExchange.API.Client.Interfaces;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Market;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Error;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using BinanceExchange.API.Websockets;
using log4net;
using Newtonsoft.Json;
using WebSocketSharp;
using System.Windows.Forms;

namespace BinanceConsole
{
    /// <summary>
    /// This Console app provides a number of examples of utilising the BinanceDotNet library
    /// </summary>
    public class ExampleProgram
    {
        public double USDBalance { get; set; }
        public double CoinBalance { get; set; }
        public double AvailableToBuy { get; set; }
        public double FeePaid { get; set; }
        public double ActualCost { get; set; }
        public double ActualBought { get; set; }
        public double BuyTarget { get; set; }
        public double SellTarget { get; set; }
        public double TradeFee = .001;
        public double PreviousCost { get; set; }
        public double lastSpent { get; set; }
        public double lastMade { get; set; }

        public static async Task Main()
        {
            //Provide your configuration and keys here, this allows the client to function as expected.
            //k: ehO2dzOvXqGxGMr0XUWEBpSwDHBGzpV3iKWaSDgIiPl2GrKyI9hKG6RWMvP7EsPF
            //sk: ff9QozCSizkxt94RATYxDToTeGicxNtuwhzhian0PxPRdPx2F4mu34tjxdvuGTqS
            string apiKey = "ehO2dzOvXqGxGMr0XUWEBpSwDHBGzpV3iKWaSDgIiPl2GrKyI9hKG6RWMvP7EsPF";
            string secretKey = "ff9QozCSizkxt94RATYxDToTeGicxNtuwhzhian0PxPRdPx2F4mu34tjxdvuGTqS";

            System.Console.WriteLine("--------------------------");
            System.Console.WriteLine("BinanceExchange API - Tester");
            System.Console.WriteLine("--------------------------");

            //Building a test logger
            var exampleProgramLogger = LogManager.GetLogger(typeof(ExampleProgram));
            exampleProgramLogger.Debug("Logging Test");

            //Initialise the general client client with config
            var client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                Logger = exampleProgramLogger,
            });
            Console.WriteLine(client);
            Console.WriteLine(apiKey);

            System.Console.WriteLine("Interacting with Binance...");

            bool DEBUG_ALL = true;

            /*
             *  Code Examples - Make sure you adjust value of DEBUG_ALL
             */
            if (DEBUG_ALL)
            {
                // Test the Client
                await client.TestConnectivity();

                // Get All Orders
                var allOrdersRequest = new AllOrdersRequest()
                {
                    Symbol = "ETHBTC",
                    Limit = 5,
                };
                allOrdersRequest = new AllOrdersRequest()
                {
                    Symbol = TradingPairSymbols.BTCPairs.ETH_BTC,
                    Limit = 5,
                };
                // Get All Orders
                //var allOrders = await client.GetAllOrders(allOrdersRequest);

                // Get the order book, and use the cache
                //var orderBook = await client.GetOrderBook("ETHBTC", true);

                // Cancel an order
                /*
                var cancelOrder = await client.CancelOrder(new CancelOrderRequest()
                {
                    NewClientOrderId = "123456",
                    OrderId = 523531,
                    OriginalClientOrderId = "789",
                    Symbol = "ETHBTC",
                });

                // Create an order with varying options
                var createOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    IcebergQuantity = 100,
                    Price = 230,
                    Quantity = 0.6m,
                    Side = OrderSide.Buy,
                    Symbol = "ETHBTC",
                    Type = OrderType.Market,
                });
                */
                // Get account information
                var accountInformation = await client.GetAccountInformation(3500);
                
                // Get account trades
                var accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                {
                    FromId = 352262,
                    Symbol = "ETHBTC",
                });
                /*
                // Get a list of Compressed aggregate trades with varying options
                var aggTrades = await client.GetCompressedAggregateTrades(new GetCompressedAggregateTradesRequest()
                {
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    Symbol = "ETHBTC",
                });

                // Get current open orders for the specified symbol
                var currentOpenOrders = await client.GetCurrentOpenOrders(new CurrentOpenOrdersRequest()
                {
                    Symbol = "ETHBTC",
                });
                */
                // Get daily ticker

                ExampleProgram ep = new ExampleProgram();
                ep.CoinBalance = 0.000000;
                ep.USDBalance = 100.00;
                ep.PreviousCost = 0.0255;
                var dailyTicker = await client.GetDailyTicker("TRXUSDT");
                var symbolOrderBookTicker = await client.GetSymbolOrderBookTicker();
                var symbolOrderPriceTicker = await client.GetSymbolsPriceTicker();

                while (true)
                {
                     dailyTicker = await client.GetDailyTicker("TRXUSDT");
                    // Get Symbol Order Book Ticket
                    symbolOrderBookTicker = await client.GetSymbolOrderBookTicker();
                    
                    // Get Symbol Order Price Ticker
                    symbolOrderPriceTicker = await client.GetSymbolsPriceTicker();

                    //get new sell target
                    ep.SellTarget = ep.GetSellTarget((decimal)ep.PreviousCost, (decimal)ep.TradeFee);

                    //get new buy target
                    ep.BuyTarget = ep.GetBuyTarget((decimal)ep.PreviousCost, (decimal)ep.TradeFee);

                    //If Buy Target Hit
                    if (dailyTicker.AskPrice <= Convert.ToDecimal(ep.BuyTarget))
                    {
                        ep.AvailableToBuy = ep.calculateAvailableToBuy(ep.USDBalance, dailyTicker.AskPrice);

                        // if available to buy > 1
                        if (ep.AvailableToBuy > 1)
                        {
                            // buy, update all vars, 
                            ep.Buy(dailyTicker.AskPrice);
                        }
                    }

                    //if sell target hit
                    if (dailyTicker.AskPrice >= Convert.ToDecimal(ep.SellTarget))
                    {
                        // if available to sell > 1
                        if (ep.CoinBalance >= 1)
                        {
                            // sell, update all vars
                            ep.Sell(dailyTicker.AskPrice);
                        }
                    }
                    Console.Clear();

                    Console.WriteLine("Ask Price: " + dailyTicker.AskPrice);
                    Console.WriteLine("Bid Price: " + dailyTicker.BidPrice);
                    Console.WriteLine("Last Price: " + dailyTicker.LastPrice);
                    Console.WriteLine("Open Price: " + dailyTicker.OpenPrice);
                    Console.WriteLine("Prev Close Price: " + dailyTicker.PreviousClosePrice);
                    Console.WriteLine();
                    Console.WriteLine("lastCost: " + ep.PreviousCost);
                    Console.WriteLine("Buy Target: " + ep.BuyTarget);
                    Console.WriteLine("Sell Target: " + ep.SellTarget);
                    Console.WriteLine("======================= BALANCES ============================");
                    Console.WriteLine("Coins: " + ep.CoinBalance);
                    Console.WriteLine("Fees Paid: " + ep.FeePaid);
                    Console.WriteLine("USD: " + ep.USDBalance);
                    Console.WriteLine("paid: " + ep.lastSpent);
                    Console.WriteLine("made: " + ep.lastMade);
                    Thread.Sleep(500);
                    /*
                    if (dailyTicker.AskPrice > Convert.ToDecimal(ep.PreviousCost + (ep.PreviousCost * ep.TradeFee)))
                    {
                        
                        Console.WriteLine("Sell Target Hit");
                        if (coinBalance > 0)
                        {
                            Console.WriteLine("Selling");
                            lastCost = Convert.ToDouble(dailyTicker.AskPrice);
                            
                            usdBalance = (coinBalance * Convert.ToDouble(dailyTicker.AskPrice))-(coinBalance * Convert.ToDouble(dailyTicker.AskPrice)*.001);
                            coinBalance = 0;
                            //sell
                            //lastcost = askprice
                        }
                    }
                    if (dailyTicker.AskPrice < Convert.ToDecimal(ep.PreviousCost + (ep.PreviousCost * ep.TradeFee)))
                    {
                        Console.WriteLine("Buy Target Hit");
                        if (usdBalance / (Convert.ToDouble(dailyTicker.AskPrice) * .001) > 0)
                        {
                            Console.WriteLine("Buying");
                            lastCost = Convert.ToDouble(dailyTicker.AskPrice);
                            
                            coinBalance = usdBalance / (Convert.ToDouble(dailyTicker.AskPrice) + (Convert.ToDouble(dailyTicker.AskPrice)*.001));
                            usdBalance -= (usdBalance / Convert.ToDouble(dailyTicker.AskPrice)) * .001; //coinBalance * (Convert.ToDouble(dailyTicker.AskPrice) * .001);
                            //buy
                            //last cost = askPrice
                        }
                    }
                    */

                }

                // Query a specific order on Binance
                var orderQuery = await client.QueryOrder(new QueryOrderRequest()
                {
                    OrderId = 5425425,
                    Symbol = "ETHBTC",
                });

                // Firing off a request and catching all the different exception types.
                try
                {
                    accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                    {
                        FromId = 352262,
                        Symbol = "ETHBTC",
                    });
                }
                catch (BinanceBadRequestException badRequestException)
                {

                }
                catch (BinanceServerException serverException)
                {

                }
                catch (BinanceTimeoutException timeoutException)
                {

                }
                catch (BinanceException unknownException)
                {

                }
            }

            // Start User Data Stream, ping and close
            var userData = await client.StartUserDataStream();
            await client.KeepAliveUserDataStream(userData.ListenKey);
            await client.CloseUserDataStream(userData.ListenKey);

            // Manual WebSocket usage
            var manualBinanceWebSocket = new InstanceBinanceWebSocketClient(client);
            var socketId = manualBinanceWebSocket.ConnectToDepthWebSocket("ETHBTC", b =>
            {
                System.Console.Clear();
                System.Console.WriteLine($"{JsonConvert.SerializeObject(b.BidDepthDeltas, Formatting.Indented)}");
                System.Console.SetWindowPosition(0, 0);
            });


            #region Advanced Examples           
            // This builds a local Kline cache, with an initial call to the API and then continues to fill
            // the cache with data from the WebSocket connection. It is quite an advanced example as it provides 
            // additional options such as an Exit Func<T> or timeout, and checks in place for cache instances. 
            // You could provide additional logic here such as populating a database, ping off more messages, or simply
            // timing out a fill for the cache.
            var dict = new Dictionary<string, KlineCacheObject>();
            //await BuildAndUpdateLocalKlineCache(client, "BNBBTC", KlineInterval.OneMinute,
            //    new GetKlinesCandlesticksRequest()
            //    {
            //        StartTime = DateTime.UtcNow.AddHours(-1),
            //        EndTime = DateTime.UtcNow,
            //        Interval = KlineInterval.OneMinute,
            //        Symbol = "BNBBTC"
            //    }, new WebSocketConnectionFunc(15000), dict);

            // This builds a local depth cache from an initial call to the API and then continues to fill 
            // the cache with data from the WebSocket
            var localDepthCache = await BuildLocalDepthCache(client);
            // Build the Buy Sell volume from the results
            var volume = ResultTransformations.CalculateTradeVolumeFromDepth("BNBBTC", localDepthCache);

            #endregion
            System.Console.WriteLine("Complete.");
            Thread.Sleep(6000);
            manualBinanceWebSocket.CloseWebSocketInstance(socketId);
            System.Console.ReadLine();
        }

        /// <summary>
        /// Build local Depth cache from WebSocket and API Call example.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, DepthCacheObject>> BuildLocalDepthCache(IBinanceClient client)
        {
            // Code example of building out a Dictionary local cache for a symbol using deltas from the WebSocket
            var localDepthCache = new Dictionary<string, DepthCacheObject> {{ "BNBBTC", new DepthCacheObject()
            {
                Asks = new Dictionary<decimal, decimal>(),
                Bids = new Dictionary<decimal, decimal>(),
            }}};
            var bnbBtcDepthCache = localDepthCache["BNBBTC"];

            // Get Order Book, and use Cache
            var depthResults = await client.GetOrderBook("BNBBTC", true, 100);
            //Populate our depth cache
            depthResults.Asks.ForEach(a =>
            {
                if (a.Quantity != 0.00000000M)
                {
                    bnbBtcDepthCache.Asks.Add(a.Price, a.Quantity);
                }
            });
            depthResults.Bids.ForEach(a =>
            {
                if (a.Quantity != 0.00000000M)
                {
                    bnbBtcDepthCache.Bids.Add(a.Price, a.Quantity);
                }
            });

            // Store the last update from our result set;
            long lastUpdateId = depthResults.LastUpdateId;
            using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(client))
            {
                binanceWebSocketClient.ConnectToDepthWebSocket("BNBBTC", data =>
                {
                    if (lastUpdateId < data.UpdateId)
                    {
                        data.BidDepthDeltas.ForEach((bd) =>
                        {
                            CorrectlyUpdateDepthCache(bd, bnbBtcDepthCache.Bids);
                        });
                        data.AskDepthDeltas.ForEach((ad) =>
                        {
                            CorrectlyUpdateDepthCache(ad, bnbBtcDepthCache.Asks);
                        });
                    }
                    lastUpdateId = data.UpdateId;
                    System.Console.Clear();
                    System.Console.WriteLine($"{JsonConvert.SerializeObject(bnbBtcDepthCache, Formatting.Indented)}");
                    System.Console.SetWindowPosition(0, 0);
                });

                Thread.Sleep(8000);
            }
            return localDepthCache;
        }

        /// <summary>
        /// Advanced approach to building local Kline Cache from WebSocket and API Call example (refactored)
        /// </summary>
        /// <param name="binanceClient">The BinanceClient instance</param>
        /// <param name="symbol">The Symbol to request</param>
        /// <param name="interval">The interval for Klines</param>
        /// <param name="klinesCandlesticksRequest">The initial request for Klines</param>
        /// <param name="webSocketConnectionFunc">The function to determine exiting the websocket (can be timeout or Func based on external params)</param>
        /// <param name="cacheObject">The cache object. Must always be provided, and can exist with data.</param>
        /// <returns></returns>
        public static async Task BuildAndUpdateLocalKlineCache(IBinanceClient binanceClient,
            string symbol,
            KlineInterval interval,
            GetKlinesCandlesticksRequest klinesCandlesticksRequest,
            WebSocketConnectionFunc webSocketConnectionFunc,
            Dictionary<string, KlineCacheObject> cacheObject)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull(webSocketConnectionFunc);
            Guard.AgainstNull(klinesCandlesticksRequest);
            Guard.AgainstNull(cacheObject);

            long epochTicks = new DateTime(1970, 1, 1).Ticks;

            if (cacheObject.ContainsKey(symbol))
            {
                if (cacheObject[symbol].KlineInterDictionary.ContainsKey(interval))
                {
                    throw new Exception(
                        "Symbol and Interval pairing already provided, please use a different interval/symbol or pair.");
                }
                cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
            }
            else
            {
                var klineCacheObject = new KlineCacheObject
                {
                    KlineInterDictionary = new Dictionary<KlineInterval, KlineIntervalCacheObject>()
                };
                cacheObject.Add(symbol, klineCacheObject);
                cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
            }

            // Get Kline Results, and use Cache
            long ticks = klinesCandlesticksRequest.StartTime.Value.Ticks;
            var startTimeKeyTime = (ticks - epochTicks) / TimeSpan.TicksPerSecond;
            var klineResults = await binanceClient.GetKlinesCandlesticks(klinesCandlesticksRequest);

            var oneMinKlineCache = cacheObject[symbol].KlineInterDictionary[interval];
            oneMinKlineCache.TimeKlineDictionary = new Dictionary<long, KlineCandleStick>();
            var instanceKlineCache = oneMinKlineCache.TimeKlineDictionary;
            //Populate our kline cache with initial results
            klineResults.ForEach(k =>
            {
                instanceKlineCache.Add(((k.OpenTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond), new KlineCandleStick()
                {
                    Close = k.Close,
                    High = k.High,
                    Low = k.Low,
                    Open = k.Open,
                    Volume = k.Volume,
                });
            });

            // Store the last update from our result set;
            using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(binanceClient))
            {
                binanceWebSocketClient.ConnectToKlineWebSocket(symbol, interval, data =>
                {
                    var keyTime = (data.Kline.StartTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond;
                    var klineObj = new KlineCandleStick()
                    {
                        Close = data.Kline.Close,
                        High = data.Kline.High,
                        Low = data.Kline.Low,
                        Open = data.Kline.Open,
                        Volume = data.Kline.Volume,
                    };
                    if (!data.Kline.IsBarFinal)
                    {
                        if (keyTime < startTimeKeyTime)
                        {
                            return;
                        }

                        TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
                    }
                    else
                    {
                        TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
                    }
                    System.Console.Clear();
                    System.Console.WriteLine($"{JsonConvert.SerializeObject(instanceKlineCache, Formatting.Indented)}");
                    System.Console.SetWindowPosition(0, 0);
                });
                if (webSocketConnectionFunc.IsTimout)
                {
                    Thread.Sleep(webSocketConnectionFunc.Timeout);
                }
                else
                {
                    while (true)
                    {
                        if (!webSocketConnectionFunc.ExitFunction())
                        {
                            // Throttle Application
                            Thread.Sleep(100);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private static void TryAddUpdateKlineCache(Dictionary<long, KlineCandleStick> primary, long keyTime, KlineCandleStick klineObj)
        {
            if (primary.ContainsKey(keyTime))
            {
                primary[keyTime] = klineObj;
            }
            else
            {
                primary.Add(keyTime, klineObj);
            }
        }

        private static void CorrectlyUpdateDepthCache(TradeResponse bd, Dictionary<decimal, decimal> depthCache)
        {
            const decimal defaultIgnoreValue = 0.00000000M;

            if (depthCache.ContainsKey(bd.Price))
            {
                if (bd.Quantity == defaultIgnoreValue)
                {
                    depthCache.Remove(bd.Price);
                }
                else
                {
                    depthCache[bd.Price] = bd.Quantity;
                }
            }
            else
            {
                if (bd.Quantity != defaultIgnoreValue)
                {
                    depthCache[bd.Price] = bd.Quantity;
                }
            }
        }

        public double GetSellTarget(decimal previous, decimal fee)
        {
            return Convert.ToDouble(previous + (previous * (fee * 2)));
        }
        public double GetBuyTarget(decimal previous, decimal fee)
        {
            return Convert.ToDouble(previous - (previous * (fee * 2)));
        }
        public double calculateAvailableToBuy(double USD, decimal askingPrice)
        {
            return USD / Convert.ToDouble(askingPrice);
        }
        public void Buy(decimal askingPrice)
        {
            FeePaid = AvailableToBuy * TradeFee;
            ActualBought = AvailableToBuy - FeePaid;
            ActualCost = USDBalance / ActualBought;
            CoinBalance = ActualBought;
            PreviousCost = ActualCost;
            USDBalance -= ActualCost * ActualBought;
            lastSpent = ActualCost * ActualBought;
        }
        public void Sell(decimal askingPrice)
        {
            FeePaid = CoinBalance * TradeFee;
            ActualBought = CoinBalance - FeePaid;
            ActualCost = CoinBalance / ActualBought;
            CoinBalance -= ActualBought;
            USDBalance += ActualBought * ActualCost;
            PreviousCost = ActualCost;
            lastMade = ActualBought * ActualCost;
        }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            ExampleProgram.Main().Wait();
        }
    }
}
