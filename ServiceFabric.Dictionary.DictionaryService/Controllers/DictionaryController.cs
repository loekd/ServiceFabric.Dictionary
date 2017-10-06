using System;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace ServiceFabric.Dictionary.DictionaryService.Controllers
{
    [Route("api/[controller]")]
    public class DictionaryController : Controller
    {
        protected StatefulServiceContext Context { get; }

        protected IReliableStateManagerReplica2 StateManager { get; }
        
        public DictionaryController(StatefulServiceContext context, IReliableStateManagerReplica2 stateManager)
        {
            Context = context;
            StateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        [HttpGet]
        public async Task<IActionResult> Get(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return BadRequest("Querystring parameter 'word' is missing");
            string meaning = await Lookup(word).ConfigureAwait(true);
            return Ok(new
            {
                word,
                meaning
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post(string word, string meaning)
        {
            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(meaning))
                return BadRequest("Querystring parameter 'word' or 'meaning' is missing");

            await Add(word, meaning).ConfigureAwait(true);
            return Accepted(new
            {
                word,
                meaning
            });
        }
        
        private async Task Add(string word, string meaning)
        {
            if (string.IsNullOrWhiteSpace(word))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(word));
            if (string.IsNullOrWhiteSpace(meaning))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(meaning));

            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(DictionaryService.MainDictionaryKey);

            using (var tx = StateManager.CreateTransaction())
            {
                await myDictionary.AddOrUpdateAsync(tx, word.ToUpperInvariant(), meaning, (key, old) => meaning);
                await tx.CommitAsync();
            }

            ServiceEventSource.Current.ServiceMessage(Context, $"{nameof(DictionaryService)} - Added or updated word '{word}' with meaning '{meaning}'.");
        }
        
        private async Task<string> Lookup(string word)
        {
            var myDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(DictionaryService.MainDictionaryKey);

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await myDictionary.TryGetValueAsync(tx, word.ToUpperInvariant());
                string meaning = result.HasValue ? result.Value : "<<<not found>>>";
                ServiceEventSource.Current.ServiceMessage(Context, $"{nameof(DictionaryService)} - Looked up word '{word}', meaning: '{meaning}'.");

                return meaning;
            }
        }
    }
}
