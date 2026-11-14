using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Npgsql;

namespace BattlegroundsGallery.Orm;

/// <summary>
/// Small custom ORM built directly on Npgsql. It maps entities using the
/// Table/Column/PrimaryKey attributes and always uses parameterized commands.
/// </summary>
public sealed class OrmSession : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;
    private bool _disposed;

    public OrmSession(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connection = new NpgsqlConnection(connectionString);
    }

    public async Task<TEntity?> FindAsync<TEntity>(
        int id,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        var metadata = EntityMetadata.For<TEntity>();
        var sql = $"SELECT * FROM {Quote(metadata.TableName)} " +
                  $"WHERE {Quote(metadata.IdProperty.ColumnName)} = @id LIMIT 1;";

        var items = await QueryAsync<TEntity>(
            sql,
            [new NpgsqlParameter("id", id)],
            cancellationToken).ConfigureAwait(false);

        return items.FirstOrDefault();
    }

    public async Task<IReadOnlyList<TEntity>> QueryAsync<TEntity>(
        string sql,
        IReadOnlyList<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var metadata = EntityMetadata.For<TEntity>();
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < reader.FieldCount; index++)
        {
            ordinals[reader.GetName(index)] = index;
        }

        var results = new List<TEntity>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var entity = Activator.CreateInstance<TEntity>()
                         ?? throw new InvalidOperationException(
                             $"Cannot create {typeof(TEntity).Name}.");

            foreach (var property in metadata.Properties)
            {
                if (!ordinals.TryGetValue(property.ColumnName, out var ordinal))
                {
                    continue;
                }

                var raw = reader.GetValue(ordinal);
                if (raw is DBNull)
                {
                    continue;
                }

                var value = ConvertValue(raw, property.Property.PropertyType);
                property.Property.SetValue(entity, value);
            }

            results.Add(entity);
        }

        return results;
    }

    public async Task<int> ExecuteAsync(
        string sql,
        IReadOnlyList<NpgsqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = CreateCommand(sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> InsertAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var metadata = EntityMetadata.For<TEntity>();
        var insertProperties = metadata.Properties
            .Where(property => property != metadata.IdProperty)
            .ToList();

        var columns = string.Join(", ", insertProperties.Select(property => Quote(property.ColumnName)));
        var values = string.Join(", ", insertProperties.Select((_, index) => $"@p{index}"));
        var sql = $"INSERT INTO {Quote(metadata.TableName)} ({columns}) " +
                  $"VALUES ({values}) RETURNING {Quote(metadata.IdProperty.ColumnName)};";

        var parameters = insertProperties
            .Select((property, index) => new NpgsqlParameter(
                $"@p{index}",
                property.Property.GetValue(entity) ?? DBNull.Value))
            .ToArray();

        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateCommand(sql, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        var id = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        metadata.IdProperty.Property.SetValue(entity, id);
        return id;
    }

    public async Task UpdateAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var metadata = EntityMetadata.For<TEntity>();
        var updateProperties = metadata.Properties
            .Where(property => property != metadata.IdProperty)
            .ToList();

        if (updateProperties.Count == 0)
        {
            return;
        }

        var assignments = string.Join(
            ", ",
            updateProperties.Select((property, index) =>
                $"{Quote(property.ColumnName)} = @p{index}"));

        var sql = $"UPDATE {Quote(metadata.TableName)} SET {assignments} " +
                  $"WHERE {Quote(metadata.IdProperty.ColumnName)} = @id;";

        var parameters = updateProperties
            .Select((property, index) => new NpgsqlParameter(
                $"@p{index}",
                property.Property.GetValue(entity) ?? DBNull.Value))
            .Append(new NpgsqlParameter(
                "@id",
                metadata.IdProperty.Property.GetValue(entity) ?? DBNull.Value))
            .ToArray();

        await ExecuteAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DeleteAsync<TEntity>(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync<TEntity>(
        int id,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        var metadata = EntityMetadata.For<TEntity>();
        var sql = $"DELETE FROM {Quote(metadata.TableName)} " +
                  $"WHERE {Quote(metadata.IdProperty.ColumnName)} = @id;";

        await ExecuteAsync(
            sql,
            [new NpgsqlParameter("id", id)],
            cancellationToken).ConfigureAwait(false);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _transaction = await _connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no active transaction.");
        }

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    private async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private NpgsqlCommand CreateCommand(
        string sql,
        IReadOnlyList<NpgsqlParameter>? parameters)
    {
        var command = new NpgsqlCommand(sql, _connection, _transaction);

        if (parameters is not null && parameters.Count > 0)
        {
            command.Parameters.AddRange(parameters.ToArray());
        }

        return command;
    }

    private static object ConvertValue(object value, Type destinationType)
    {
        var targetType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(string))
        {
            return value.ToString() ?? string.Empty;
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value.ToString() ?? string.Empty, ignoreCase: true);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static string Quote(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_transaction is not null)
        {
            try
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
            }
            catch
            {
                // The connection may already be broken.
            }

            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
