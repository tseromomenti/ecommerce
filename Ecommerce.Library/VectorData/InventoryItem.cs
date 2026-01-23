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
        public string ItemName { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true, StorageName = "description")]
        public string Description { get; set; } = string.Empty;

        [VectorStoreData(IsIndexed = true, StorageName = "price")]
        public int Price { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "available_stock")]
        public int AvailableStock { get; set; }

        /// <summary>
        /// 768 dimensions for nomic-embed-text model
        /// </summary>
        [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "item_embedding")]
        public ReadOnlyMemory<float>? ItemEmbedding { get; set; }
    }
}
