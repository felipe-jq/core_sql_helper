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
