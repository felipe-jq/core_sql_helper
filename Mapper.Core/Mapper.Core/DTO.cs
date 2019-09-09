using Mapper.Core.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Mapper.Core
{
  public static class DTO
  {
    /// <summary>
    /// Excecute sql command with query
    /// </summary>
    /// <typeparam name="T">Type of return class</typeparam>
    /// <param name="pConnection">Database conection</param>
    /// <param name="pSPName">Store procedure name</param>
    /// <param name="pParamValues">All values of params to invoke sp</param>
    /// <returns>DTO enumerable class with values</returns>
    public static IEnumerable<T> ExecuteSql<T>(string pConnection, string pSPName, params object[] pParamValues)
    {
      try
      {
        using (var con = new SqlConnection(pConnection))
        {
          con.Open();

          using (var command = new SqlCommand(pSPName, con))
          {
            command.CommandType = CommandType.StoredProcedure;
            SqlCommandBuilder.DeriveParameters(command);  //Derive parameters for make autodiscovery
            for (int i = 0; i < command.Parameters.Count; i++)
            {
              command.Parameters[i].Value = pParamValues[i];
            }

            var dataReader = command.ExecuteReader();
            var list = Get<T>(dataReader);
            return list;
          }
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    /// <summary>
    /// Execute SQL and map retur to T class
    /// </summary>
    /// <typeparam name="T">Class with params returned</typeparam>
    /// <param name="pConnection">DB Conection string</param>
    /// <param name="pSPName">Store procedure name</param>
    /// <param name="pParamValues">Values of params to execute sp</param>
    /// <returns>DTO mapped class</returns>
    public static T ExecuteSqlNonQuery<T>(string pConnection, string pSPName, params object[] pParamValues)
    {
      using (var con = new SqlConnection(pConnection))
      {
        con.Open();

        using (var command = new SqlCommand(pSPName, con))
        {
          command.CommandType = CommandType.StoredProcedure;
          SqlCommandBuilder.DeriveParameters(command);  //Derive parameters for make autodiscovery
          for (int i = 0; i < command.Parameters.Count; i++)
          {
            if (command.Parameters[i].Direction == ParameterDirection.Input)
              command.Parameters[i].Value = pParamValues[i];

            if (command.Parameters[i].Direction == ParameterDirection.InputOutput)
              command.Parameters[i].Direction = ParameterDirection.Output;
          }

          command.ExecuteNonQuery();
          var element = (T)Activator.CreateInstance(typeof(T));
          var propiedades = element.GetType().GetProperties();
          foreach (var item in propiedades)
          {
            var columna = item.GetCustomAttributes(false).FirstOrDefault().ToString();
            var valor = command.Parameters[columna].Value;
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
