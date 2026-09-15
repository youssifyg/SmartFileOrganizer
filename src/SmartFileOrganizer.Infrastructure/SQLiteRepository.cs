#pragma warning disable CS8603, CS8605, CS8601, CS8604
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure
{
    public class SQLiteRepository : IRepository
    {
        private readonly string _connectionString;
        private bool _initialized = false;

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (!_initialized)
            {
                await InitializeAsync(cancellationToken);
                _initialized = true;
            }
        }

        public SQLiteRepository(string dbPath)
        {
            if (string.Equals(dbPath, ":memory:", StringComparison.OrdinalIgnoreCase))
            {
                _connectionString = "Data Source=:memory:;Mode=Memory;Cache=Shared;Pooling=False";
            }
            else
            {
                var fullPath = Path.GetFullPath(dbPath);
                _connectionString = $"Data Source={fullPath};Pooling=False";
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var createStatements = new[]
            {
                @"CREATE TABLE IF NOT EXISTS FileRecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, Path TEXT NOT NULL, Size INTEGER NOT NULL, Created TEXT NOT NULL, Modified TEXT NOT NULL);",
                @"CREATE TABLE IF NOT EXISTS DuplicateGroups (Id INTEGER PRIMARY KEY AUTOINCREMENT, TotalSize INTEGER NOT NULL, RecoverableSize INTEGER NOT NULL);",
                @"CREATE TABLE IF NOT EXISTS DuplicateGroupFiles (GroupId INTEGER NOT NULL, Path TEXT NOT NULL, FOREIGN KEY(GroupId) REFERENCES DuplicateGroups(Id));",
                @"CREATE TABLE IF NOT EXISTS ScanSessions (Id INTEGER PRIMARY KEY AUTOINCREMENT, StartedAt TEXT NOT NULL, CompletedAt TEXT);",
                @"CREATE TABLE IF NOT EXISTS HashCaches (Id INTEGER PRIMARY KEY AUTOINCREMENT, Path TEXT NOT NULL, Size INTEGER NOT NULL, LastModified TEXT NOT NULL, PartialHash BLOB, FullHash BLOB, UNIQUE(Path, Size, LastModified));",
                @"CREATE TABLE IF NOT EXISTS OperationHistories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Action TEXT NOT NULL, FilePath TEXT NOT NULL, Timestamp TEXT NOT NULL, Size INTEGER NOT NULL);"
            };

            foreach (var stmt in createStatements)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = stmt;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task SaveFileRecordAsync(FileRecord record, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO FileRecords (Path, Size, Created, Modified) VALUES (@path, @size, @created, @modified);";
            if (record.Path == null) throw new ArgumentNullException(nameof(record.Path));
            cmd.Parameters.AddWithValue("@path", record.Path);
            cmd.Parameters.AddWithValue("@size", record.Size);
            cmd.Parameters.AddWithValue("@created", record.Created.ToString("o"));
            cmd.Parameters.AddWithValue("@modified", record.Modified.ToString("o"));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<FileRecord>> GetAllFileRecordsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            var list = new List<FileRecord>();
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Path, Size, Created, Modified FROM FileRecords;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var rec = new FileRecord
                {
                    Path = reader.GetString(0),
                    Size = reader.GetInt64(1),
                    Created = DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    Modified = DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
                list.Add(rec);
            }
            return list;
        }

        public async Task SaveDuplicateGroupAsync(DuplicateGroup group, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var tx = connection.BeginTransaction();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO DuplicateGroups (TotalSize, RecoverableSize) VALUES (@total, @recover); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@total", group.TotalSize);
            cmd.Parameters.AddWithValue("@recover", group.RecoverableSize);
            var groupId = (long)await cmd.ExecuteScalarAsync(cancellationToken);
            foreach (var file in group.Files)
            {
                var fileCmd = connection.CreateCommand();
                fileCmd.CommandText = "INSERT INTO DuplicateGroupFiles (GroupId, Path) VALUES (@gid, @path);";
                fileCmd.Parameters.AddWithValue("@gid", groupId);
                fileCmd.Parameters.AddWithValue("@path", file.Path);
                await fileCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            var groups = new List<DuplicateGroup>();
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, TotalSize, RecoverableSize FROM DuplicateGroups;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var groupMap = new Dictionary<int, DuplicateGroup>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = (int)reader.GetInt64(0);
                var grp = new DuplicateGroup
                {
                    Id = id,
                    Files = new List<FileRecord>(),
                    TotalSize = reader.GetInt64(1),
                    RecoverableSize = reader.GetInt64(2)
                };
                groupMap[id] = grp;
                groups.Add(grp);
            }
            var fileCmd = connection.CreateCommand();
            fileCmd.CommandText = "SELECT GroupId, Path FROM DuplicateGroupFiles;";
            await using var fileReader = await fileCmd.ExecuteReaderAsync(cancellationToken);
            while (await fileReader.ReadAsync(cancellationToken))
            {
                var gid = (int)fileReader.GetInt64(0);
                var path = fileReader.GetString(1);
                if (groupMap.TryGetValue(gid, out var grp))
                {
                    ((List<FileRecord>)grp.Files).Add(new FileRecord { Path = path });
                }
            }
            return groups;
        }

        public async Task SaveScanSessionAsync(ScanSession session, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO ScanSessions (StartedAt, CompletedAt) VALUES (@start, @end);";
            cmd.Parameters.AddWithValue("@start", session.StartedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@end", session.CompletedAt?.ToString("o"));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<ScanSession> GetLatestScanSessionAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, StartedAt, CompletedAt FROM ScanSessions ORDER BY Id DESC LIMIT 1;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new ScanSession
                {
                    Id = (int)reader.GetInt64(0),
                    StartedAt = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    CompletedAt = string.IsNullOrEmpty(reader.GetString(2)) ? (DateTime?)null : DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
            }
            return null;
        }

        public async Task SaveHashCacheAsync(HashCache cache, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO HashCaches (Path, Size, LastModified, PartialHash, FullHash)
                VALUES (@path, @size, @lm, @ph, @fh);";
            cmd.Parameters.AddWithValue("@path", cache.Path);
            cmd.Parameters.AddWithValue("@size", cache.Size);
            cmd.Parameters.AddWithValue("@lm", cache.LastModified.ToString("o"));
            cmd.Parameters.AddWithValue("@ph", cache.PartialHash ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fh", cache.FullHash ?? (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<HashCache> GetHashCacheAsync(string path, long size, DateTime lastModified, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id, PartialHash, FullHash FROM HashCaches WHERE Path = @path AND Size = @size AND LastModified = @lm;";
            cmd.Parameters.AddWithValue("@path", path);
            cmd.Parameters.AddWithValue("@size", size);
            cmd.Parameters.AddWithValue("@lm", lastModified.ToString("o"));
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new HashCache
                {
                    Id = reader.GetInt64(0),
                    Path = path,
                    Size = size,
                    LastModified = lastModified,
                    PartialHash = reader.IsDBNull(1) ? null : (byte[])reader[1],
                    FullHash = reader.IsDBNull(2) ? null : (byte[])reader[2]
                };
            }
            return null;
        }

        public async Task SaveOperationHistoryAsync(OperationHistoryEntry entry, CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO OperationHistories (Action, FilePath, Timestamp, Size) VALUES (@action, @path, @ts, @size);";
            cmd.Parameters.AddWithValue("@action", entry.Action);
            cmd.Parameters.AddWithValue("@path", entry.FilePath);
            cmd.Parameters.AddWithValue("@ts", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@size", entry.Size);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OperationHistoryEntry>> GetOperationHistoryAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<OperationHistoryEntry>();
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Action, FilePath, Timestamp, Size FROM OperationHistories ORDER BY Id;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new OperationHistoryEntry
                {
                    Id = (int)reader.GetInt64(0),
                    Action = reader.GetString(1),
                    FilePath = reader.GetString(2),
                    Timestamp = DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    Size = reader.GetInt64(4)
                });
            }
            return list;
        }
    }
}
