﻿using System;

namespace Mapper.Core
{
  public sealed class DBEntity : Attribute
  {
    public DBEntity(string colum)
    {
      DbColum = colum;
    }

    public override string ToString()
    {
      return DbColum;
    }

    public string DbColum { get; }
  }
}
