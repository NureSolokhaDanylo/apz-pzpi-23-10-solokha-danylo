using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NotionArchitecture.ComplexExample
{
    public class Block
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int Version { get; set; }

        public Block Clone() => new Block { Id = Id, Content = Content, Version = Version };
    }

    public record Operation(Guid BlockId, string NewContent, int BaseVersion);

    public class NotionSyncEngine
    {
        private readonly ConcurrentDictionary<Guid, Block> _clientCache = new();
        private readonly ConcurrentQueue<Operation> _transactionQueue = new();
        private readonly Dictionary<Guid, Block> _serverDb = new(); 
        
        public NotionSyncEngine()
        {
            var rootId = Guid.NewGuid();
            _serverDb[rootId] = new Block { Id = rootId, Content = "Initial Content", Version = 1 };
            _clientCache[rootId] = _serverDb[rootId].Clone();
        }

        public void EditBlock(Guid id, string newContent)
        {
            if (!_clientCache.TryGetValue(id, out var block)) return;

            var baseVersion = block.Version;
            block.Content = newContent; 
            
            _transactionQueue.Enqueue(new Operation(id, newContent, baseVersion));
            
            Console.WriteLine($"[UI] Block {id} updated optimistically: '{newContent}' (v{baseVersion})");
        }

        public async Task StartSyncLoop(CancellationToken ct)
        {
            Console.WriteLine("[System] Background sync started...");

            while (!ct.IsCancellationRequested)
            {
                if (_transactionQueue.TryDequeue(out var op))
                {
                    await ProcessOperation(op);
                }
                await Task.Delay(500); 
            }
        }

        private async Task ProcessOperation(Operation op)
        {
            Console.WriteLine($"[Network] Sending transaction to server for block {op.BlockId}...");
            await Task.Delay(1000); 

            lock (_serverDb)
            {
                var serverBlock = _serverDb[op.BlockId];

                if (serverBlock.Version == op.BaseVersion)
                {
                    serverBlock.Content = op.NewContent;
                    serverBlock.Version++;
                    
                    _clientCache[op.BlockId].Version = serverBlock.Version;
                    Console.WriteLine($"[Server] TRANSACTION COMMITTED. New version: v{serverBlock.Version}");
                }
                else
                {
                    Console.WriteLine($"[Conflict] Sync Error! Server has v{serverBlock.Version}, client sent v{op.BaseVersion}");
                    
                    _clientCache[op.BlockId] = serverBlock.Clone();
                    Console.WriteLine($"[Client] Block {op.BlockId} state synchronized with server (ROLLBACK/UPDATE)");
                }
            }
        }

        public void DisplayState()
        {
            Console.WriteLine("\n--- CURRENT STATE ---");
            foreach (var b in _clientCache.Values)
                Console.WriteLine($"Block: {b.Id} | Content: {b.Content} | Version: v{b.Version}");
            Console.WriteLine("----------------------\n");
        }
    }

    public class Program
    {
        public static async Task Main()
        {
            var engine = new NotionSyncEngine();
            var cts = new CancellationTokenSource();

            var syncTask = engine.StartSyncLoop(cts.Token);

            // Simulation logic
            // In a real scenario, you would obtain a valid ID from the engine
            // Guid targetId = ...;
            
            await Task.Delay(3000);
            engine.DisplayState();

            cts.Cancel();
            try { await syncTask; } catch (OperationCanceledException) {}
        }
    }
}
