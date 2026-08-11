using Compliance.MasterData.Contracts;
using Compliance.MasterData.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Compliance.MasterData.Providers
{
    public sealed class FileBasedMasterDataProvider : IMasterDataProvider
    {
        private readonly string _manifestPath;
        private readonly string _dataPath;

        public FileBasedMasterDataProvider(
            string manifestPath,
            string dataPath)
        {
            _manifestPath = manifestPath
                ?? throw new ArgumentNullException(nameof(manifestPath));
            _dataPath = dataPath
                ?? throw new ArgumentNullException(nameof(dataPath));
        }

        public MasterDataManifest GetManifest()
        {
            if (!File.Exists(_manifestPath))
                throw new FileNotFoundException(
                    $"Master data manifest not found at path: {_manifestPath}");

            var json = File.ReadAllText(_manifestPath);

            var manifest = JsonSerializer.Deserialize<MasterDataManifest>(
                json,
                JsonOptions());

            if (manifest == null)
                throw new InvalidOperationException(
                    "Failed to deserialize master data manifest.");

            return manifest;
        }

        public MasterDataRootDto GetData()
        {
            if (!File.Exists(_dataPath))
                throw new FileNotFoundException(
                    $"Master data file not found at path: {_dataPath}");

            var json = File.ReadAllText(_dataPath);

            var data = JsonSerializer.Deserialize<MasterDataRootDto>(
                json,
                JsonOptions());

            if (data == null)
                throw new InvalidOperationException(
                    "Failed to deserialize master data file.");

            return data;
        }

        private static JsonSerializerOptions JsonOptions()
            => new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
    }

}
