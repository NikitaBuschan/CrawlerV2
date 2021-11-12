using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CrawlerLogSaver.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CrawlerLogSaver
{
    public class Function
    {
        public async Task<string> FunctionHandler(SaveLog saveLog, ILambdaContext context)
        {
            Lambda._context = context;
            Lambda.Log($"Gettings data {saveLog.Contract.Address}");


            // work with transaction
            var transaction = new Transaction() { Hash = saveLog.Hash };
            int transactionId = 0;

            var result = await IsContainedInDb("transaction", transaction);

            if (result == null)
            {
                Lambda.Log($"Transaction {saveLog.Hash} is not contained in DB");

                transaction = CreateTransactionObject(saveLog);
                transactionId = Convert.ToInt32(await WriteToDb("transaction", transaction));

                Lambda.Log($"Save transaction with ID: {transactionId}");
            }
            else
            {
                Lambda.Log($"Transaction {saveLog.Hash} is contained in DB");
                transactionId = JsonSerializer.Deserialize<Transaction>(result).Id;
            }


            // work with log
            var log = CreateLogObject(transactionId, saveLog);
            int logId = 0;

            result = await IsContainedInDb("log", log);

            if (result == null)
            {
                Lambda.Log($"Log from transactionId {log.TransactionId} is not contained in DB");
                logId = Convert.ToInt32(await WriteToDb("log", log));

                Lambda.Log($"Save Log with ID: {logId}");
            }
            else
            {
                Lambda.Log($"Log from transactionId {log.TransactionId} is contained in DB");
                logId = JsonSerializer.Deserialize<Log>(result).Id;
            }


            // work with log data
            var logData = CreateLogDataObject(logId, saveLog);
            int logDataId = 0;

            result = await IsContainedInDb("logData", logData);

            if (result == null)
            {
                Lambda.Log($"LogData from logId {logData.LogId} is not contained in DB");
                logDataId = Convert.ToInt32(await WriteToDb("logData", logData));

                Lambda.Log($"Save LogData with ID: {logDataId}");
            }
            else
            {
                Lambda.Log($"LogData from logId {logData.LogId} is contained in DB");
                logDataId = JsonSerializer.Deserialize<LogData>(result).Id;
            }

            var contract = saveLog.Contract;

            if (logDataId != 0)
            {
                if (saveLog.Type == 0)
                {
                    // RPC
                    Lambda.Log($"Last block RPC: {contract.LastBlockRPC}");
                    Lambda.Log($"Log block number: {log.BlockNumber}");

                    if (contract.LastBlockRPC <= Convert.ToInt32(log.BlockNumber))
                    {
                        contract.LastBlockRPC = Convert.ToInt32(log.BlockNumber) + 1;

                        Lambda.Log($"Update last block RPC to {contract.LastBlockRPC}");
                        await UpdateInDb("contract", contract);
                    }
                }
                else
                {
                    // Covelent
                    Lambda.Log($"Last block Covalent {contract.LastBlockWS}");
                    Lambda.Log($"Log block number: {log.BlockNumber}");

                    if (contract.LastBlockWS <= Convert.ToInt32(log.BlockNumber))
                    {
                        contract.LastBlockWS = Convert.ToInt32(log.BlockNumber) + 1;

                        Lambda.Log($"Update last block Covalent to {contract.LastBlockWS}");
                        await UpdateInDb("contract", contract);
                    }
                }
            }

            return $"Save log";
        }

        public async Task<string> IsContainedInDb<T>(string name, T sendData)
        {
            var data = new DbObject()
            {
                Name = name,
                Data = JsonSerializer.Serialize(sendData)
            };

            Lambda.Log($"Checking object in DB");

            return await Lambda.Run("DBReader", JsonSerializer.Serialize(data));
        }

        public async Task<string> WriteToDb<T>(string name, T sendData)
        {
            var data = new DbObject()
            {
                Name = name,
                Data = JsonSerializer.Serialize(sendData)
            };

            Lambda.Log($"Write {name} to DB");

            return await Lambda.Run("DBWriter", JsonSerializer.Serialize(data));
        }

        public async Task<string> UpdateInDb<T>(string name, T sendData)
        {
            var data = new DbObject()
            {
                Name = name,
                Data = JsonSerializer.Serialize(sendData)
            };

            //Lambda.Log($"Update {name} in DB from {from}, to {to}");

            return await Lambda.Run("LambdaDbUpdater", JsonSerializer.Serialize(data));
        }

        public TransactionData CreateTransactionDataObject(string from, string to, string value, string hash) =>
            new TransactionData()
            {
                From = from,
                To = to,
                Hash = hash,
                Value = BigInteger.Parse(value)
            };

        public Transaction CreateTransactionObject(SaveLog log) =>
            new Transaction()
            {
                ContractId = log.Contract.Id,
                Hash = log.Hash,
                Object = JsonSerializer.Serialize(CreateTransactionDataObject(log.From, log.To, log.Value, log.Hash))
            };

        public Log CreateLogObject(int transactionId, SaveLog log) =>
            new Log()
            {
                TransactionId = transactionId,
                BlockHash = log.BlockHash,
                BlockNumber = log.BlockNumber,
                Removed = log.Removed,
                TransactionIndex = log.TransactionIndex
            };

        public LogData CreateLogDataObject(int logId, SaveLog log) =>
            new LogData()
            {
                LogId = logId,
                Address = log.Contract.Address,
                Data = log.Data,
                LogIndex = log.LogIndex,
                Topics = CreateTopicString(log.Topics)
            };

        public string CreateTopicString(List<string> Topics)
        {
            var topics = "";
            for (int i = 0; i < Topics.Count; i++)
            {
                if (i != Topics.Count - 1)
                {
                    topics += $"{Topics[i]},";
                }
                else
                {
                    topics += Topics[i];
                }
            }

            return topics;
        }
    }
}

