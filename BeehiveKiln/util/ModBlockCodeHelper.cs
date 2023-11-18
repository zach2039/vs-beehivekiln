using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace beehivekiln.util
{
    public class ModBlockCodeHelper
    {
        public static void ReplaceBlockCodeInMultiblockStructure(ref MultiblockStructure ms, AssetLocation search, AssetLocation replace)
        {
            Dictionary<AssetLocation, AssetLocation> keyChangeDict = new Dictionary<AssetLocation, AssetLocation>();

            // Scan first
            foreach (KeyValuePair<AssetLocation, int> entry in ms.BlockNumbers)
            {
                AssetLocation newKey = entry.Key.WildCardReplace(search, replace);

                if (newKey != null)
                    keyChangeDict.Add(entry.Key, newKey);
            }

            // Then remap
            foreach (KeyValuePair<AssetLocation, AssetLocation> keyMapping in keyChangeDict)
            {
                DictionaryExtensions.ChangeKeyInDict(ms.BlockNumbers, keyMapping.Key, keyMapping.Value);
            }
        }
    }
}