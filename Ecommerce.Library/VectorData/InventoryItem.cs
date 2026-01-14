using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData;

namespace Ecommerce.Library.VectorData
{
    public class InventoryItem
    {
        [VectorStoreKey]
        public ulong ItemId { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "item_name")]
        public string ItemName { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "price")]
        public decimal Price { get; set; }

        [VectorStoreVector(4, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "item_description_embedding")]
        public ReadOnlyMemory<float>? ItemDescriptionEmbedding { get; set; }
    }
}
