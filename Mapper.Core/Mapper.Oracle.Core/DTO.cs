using Mapper.Oracle.Core.Properties;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Mapper.Oracle.Core
{
  public static class DTO
  {
    public static IEnumerable<T> ExecuteOracle<T>(string spName, IEnumerable<Param> paramList, string conString)
    {
      using (var con = new OracleConnection(conString))
      {
        using (var cmd = con.CreateCommand())
        {
          con.Open();
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = spName;

          foreach (var item in paramList)
          {
            cmd.Parameters.Add(item.OracleParam);
          }

          var dataReader = cmd.ExecuteReader();
          var list = Get<T>(dataReader);
          return list;

        }
      }
    }

    public static T ExecuteOracleNonQuery<T>(string spName, IEnumerable<Param> paramList, string conString)
    {
      using (var con = new OracleConnection(conString))
      {
        using (var cmd = con.CreateCommand())
        {
          con.Open();
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = spName;

          foreach (var item in paramList)
          {
            cmd.Parameters.Add(item.OracleParam);
          }

          cmd.ExecuteNonQuery();
          var element = (T)Activator.CreateInstance(typeof(T));
          var propiedades = element.GetType().GetProperties();
          foreach (var item in propiedades)
          {
            var columna = item.GetCustomAttributes(false).FirstOrDefault().ToString();
            var valor = cmd.Parameters[columna].Value;
            item.SetValue(element, Converter(valor, item.PropertyType), null);
          }

          return element;

        }
      }
    }

    public static IEnumerable<T> Get<T>(IDataReader pDataReader)
    {
      try
      {
        var list = new List<T>();

        while (pDataReader.Read())
        {
          var element = (T)Activator.CreateInstance(typeof(T));
          var properties = element.GetType().GetProperties();
          foreach (var item in properties)
          {
            var columna = GetColumnName(item);
            if (columna != null)
            {
              var valor = pDataReader[columna];
              item.SetValue(element, Converter(valor, item.PropertyType), null);
            }
          }

          list.Add(element);
        }

        return list.Count > 0 ? list : null;
      }
      catch (IndexOutOfRangeException ex)
      {
        throw new InvalidOperationException(string.Format(Resources.MsErrorOutOfRange, ex.Message));
      }
      catch (Exception)
      {
        throw;
      }
    }

    private static object Converter(object pValue, Type pType)
    {
      try
      {
        if (pValue == DBNull.Value)
          // En caso de que sea db null se crea una instancia generica
          if (pType == typeof(string))
            return string.Empty;
          else
            return Activator.CreateInstance(pType);

        return Convert.ChangeType(pValue, pType);
      }
      catch (Exception)
      {
        throw new InvalidOperationException(string.Format(Resources.MsErrorConver, pValue, pType.FullName));
      }
    }

    private static string GetColumnName(PropertyInfo pProperty)
    {
      var attribute = pProperty.GetCustomAttributes(false).FirstOrDefault();
      if (attribute != null)
        return attribute.ToString();
      else
        return null;
    }
  }
}

