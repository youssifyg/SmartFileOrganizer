using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;

namespace SmartFileOrganizer.Infrastructure
{
    public class HashService : IHashService
    {
        public async Task<byte[]> CalculatePartialHashAsync(string filePath, int chunkSize = 4096, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("File path must be provided", nameof(filePath));

                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                int readSize = Math.Min(chunkSize, (int)stream.Length);
                var buffer = new byte[readSize];
                int bytesRead = await stream.ReadAsync(buffer, 0, readSize, cancellationToken);
                using var sha = SHA256.Create();
                return sha.ComputeHash(buffer, 0, bytesRead);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public async Task<byte[]> CalculateFullHashAsync(string filePath, CancellationToken cancellationToken = default)
        {
    try
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be provided", nameof(filePath));

        using var sha = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 81920, useAsync: true);
        // Compute hash by streaming
        var hash = await Task.Run(() =>
        {
            var buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                sha.TransformBlock(buffer, 0, read, null, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return sha.Hash;
        }, cancellationToken);
        return hash;
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (IOException)
    {
        return null;
    }
    catch (UnauthorizedAccessException)
    {
        return null;
    }
}
    }
}
