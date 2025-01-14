﻿using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmTest.Test
{
    public class BugTest
    {
        public static void Init()
        {
            SqlSugarClient Db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = @"PORT=5433;DATABASE=x;HOST=localhost;PASSWORD=Gauss666;USER ID=gauss",
                DbType =DbType.OpenGauss,
                IsAutoCloseConnection = true,
           
                InitKeyType = InitKeyType.Attribute,
            });
            //调式代码 用来打印SQL 
            Db.Aop.OnLogExecuting = (sql, pars) =>
            {
               Console.WriteLine(sql);
            };

            Db.CodeFirst.InitTables(typeof(testmmmm12));

            var id = Db.Insertable(new testmmmm12 { name="a", isok=true }).ExecuteReturnIdentity();
            var list = Db.Queryable<testmmmm12>().InSingle(id);
            var x = Db.Queryable<testmmmm12>().Where(it =>   !it.isok).ToList();
 
        }
    }

    public class testmmmm12
    {
        [SugarColumn(IsPrimaryKey =true,IsIdentity =true)]
        public int id { get; set; }
        public string name { get; set; }
        public bool isok { get; set; }
    }

}
