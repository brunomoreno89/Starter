/*
    1.4. DataReaderExtensions (ajuda para o mapeamento)
        Arquivo: Extensions/DataReaderExtensions.cs
        Função: ter um helper para saber se uma coluna existe no resultado da SP.
*/

using System.Data;

namespace Starter.Api.Extensions;

public static class DataReaderExtensions
{
    public static bool HasColumn(this IDataRecord reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
