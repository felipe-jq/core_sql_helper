using Oracle.ManagedDataAccess.Client;

namespace Mapper.Oracle.Core
{
  public class Param
  {
    public Param(string name, object val)
    {
      Name = name;
      Val = val;
      OracleParam = new OracleParameter(name, val);
    }

    public Param(string name, OracleDbType DbType, int Size)
    {
      Name = name;
      OracleParam = new OracleParameter(name, DbType, Size, null, System.Data.ParameterDirection.Output);
    }

    public string Name { get; set; }
    public object Val { get; set; }
    public int Size { get; set; }
    public OracleParameter OracleParam { get; }
  }
}
