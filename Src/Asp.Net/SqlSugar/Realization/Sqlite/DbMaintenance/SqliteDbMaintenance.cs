﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlSugar
{
    public class SqliteDbMaintenance : DbMaintenanceProvider
    {
        #region DML
        protected override string GetDataBaseSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        protected override string GetColumnInfosByTableNameSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        protected override string GetTableInfoListSql
        {
            get
            {
                return @"select Name from sqlite_master where type='table' and name<>'sqlite_sequence' order by name;";
            }
        }
        protected override string GetViewInfoListSql
        {
            get
            {
                return @"select Name from sqlite_master where type='view'  order by name;";
            }
        }
        #endregion

        #region DDL
        protected override string CreateDataBaseSql
        {
            get
            {
                return "CREATE DATABASE {0}";
            }
        }
        protected override string AddPrimaryKeySql
        {
            get
            {
                return "";
            }
        }
        protected override string AddColumnToTableSql
        {
            get
            {
                return "ALTER TABLE {0} ADD COLUMN {1} {2}{3}";
            }
        }
        protected override string AlterColumnToTableSql
        {
            get
            {
                // return "ALTER TABLE {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}";
                throw new NotSupportedException();
            }
        }
        protected override string BackupDataBaseSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        protected override string CreateTableSql
        {
            get
            {
                return "CREATE TABLE {0}(\r\n{1} $PrimaryKey )";
            }
        }
        protected override string CreateTableColumn
        {
            get
            {
                return "{0} {1}{2} {3} {4} {5}";
            }
        }
        protected override string TruncateTableSql
        {
            get
            {
                return "DELETE FROM {0}";
            }
        }
        protected override string BackupTableSql
        {
            get
            {
                return " CREATE TABLE {0} AS SELECT * FROM {1} limit 0,{2}";
            }
        }
        protected override string DropTableSql
        {
            get
            {
                return "DROP TABLE {0}";
            }
        }
        protected override string DropColumnToTableSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        protected override string DropConstraintSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        protected override string RenameColumnSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string AddColumnRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string DeleteColumnRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string IsAnyColumnRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string AddTableRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string DeleteTableRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string IsAnyTableRemarkSql
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        protected override string RenameTableSql
        {
            get
            {
                return "alter table  {0} rename to {1}";
            }
        }

        protected override string CreateIndexSql
        {
            get
            {
                return "CREATE {3} INDEX Index_{0}_{2} ON {0}({1})";
            }
        }
        protected override string AddDefaultValueSql
        {
            get
            {
                throw new NotSupportedException(" Sqlite no support default value");
            }
        }
        protected override string IsAnyIndexSql
        {
            get
            {
                return "SELECT count(*) FROM sqlite_master WHERE name = '{0}'";
            }
        }
        #endregion

        #region Check
        protected override string CheckSystemTablePermissionsSql
        {
            get
            {
                return "select Name from sqlite_master limit 0,1";
            }
        }
        #endregion

        #region Scattered
        protected override string CreateTableNull
        {
            get
            {
                return "NULL";
            }
        }
        protected override string CreateTableNotNull
        {
            get
            {
                return "NOT NULL";
            }
        }
        protected override string CreateTablePirmaryKey
        {
            get
            {
                return "PRIMARY KEY";
            }
        }
        protected override string CreateTableIdentity
        {
            get
            {
                return "AUTOINCREMENT";
            }
        }
        #endregion

        #region Methods
        public override void AddDefaultValue(EntityInfo entityInfo)
        {
            Console.WriteLine("sqlite no support AddDefaultValue");
        }
        public override bool AddDefaultValue(string tableName, string columnName, string defaultValue)
        {
            Console.WriteLine("sqlite no support AddDefaultValue");
            return true;
        }
        public override bool TruncateTable(string tableName)
        {
            base.TruncateTable(tableName);//delete data
            try
            {
                //clear sqlite  identity
                return this.Context.Ado.ExecuteCommand($"UPDATE sqlite_sequence SET seq = 0 WHERE name = '{tableName}'") > 0;
            }
            catch
            {
                //if no identity sqlite_sequence
                return true;
            }
        }
        /// <summary>
        ///by current connection string
        /// </summary>
        /// <param name="databaseDirectory"></param>
        /// <returns></returns>
        public override bool CreateDatabase(string databaseName, string databaseDirectory = null)
        {
            var connString = this.Context.CurrentConnectionConfig.ConnectionString;
            var linuxPath = connString.Split('=')?[1]?.TrimStart();
            if (connString == null) 
            {
                throw new Exception("ConnectionString is null");
            }
            var path = Regex.Match(connString, @"[a-z,A-Z]\:\\.+\\").Value;
            if (linuxPath.StartsWith("."))
            {//当前目录写法 . 号
                var fileInfo = new System.IO.FileInfo(System.IO.Path.Combine(Environment.CurrentDirectory, linuxPath));
                path = fileInfo.DirectoryName;
            }
            if (path.IsNullOrEmpty())
            {
                path = Regex.Match(connString, @"\/.+\/").Value;
            }
            if (path.IsNullOrEmpty())
            {
                path = Regex.Match(connString, @"[a-z,A-Z]\:\\").Value;
            }
            if (!FileHelper.IsExistDirectory(path))
            {
                FileHelper.CreateDirectory(path);
            }
            this.Context.Ado.Connection.Open();
            this.Context.Ado.Connection.Close();
            return true;
        }
        public override List<DbColumnInfo> GetColumnInfosByTableName(string tableName, bool isCache = true)
        {
            string cacheKey = "DbMaintenanceProvider.GetColumnInfosByTableName." + this.SqlBuilder.GetNoTranslationColumnName(tableName).ToLower();
            cacheKey = GetCacheKey(cacheKey);
            if (!isCache)
            {
                return GetColumnsByTableName(tableName);
            }
            return this.Context.Utilities.GetReflectionInoCacheInstance().GetOrCreate(cacheKey,
            () =>
                 {
                     return GetColumnsByTableName(tableName);

                 });
        }
        public override bool AddRemark(EntityInfo entity)
        {
            return true;
        }
        // Sqlite 没有表列注释功能
        public override bool IsAnyTableRemark(string tableName)
        {
            return false;
        }
        public override bool IsAnyColumnRemark(string columnName, string tableName)
        {
            return false;
        }
        public override bool AddTableRemark(string tableName, string description)
        {
            return true;
        }
        public override bool AddColumnRemark(string columnName, string tableName, string description)
        {
            return true;
        }
        public override bool DeleteTableRemark(string tableName)
        {
            return true;
        }
        public override bool DeleteColumnRemark(string columnName, string tableName)
        {
            return true;
        }
        public override bool DropColumn(string tableName, string columnName)
        {
            // 目前Sqlite 没有删除列功能,使用复制功能实现
            var columns = GetColumnInfosByTableName(tableName, false);
            columns.Remove(columns.FirstOrDefault(m => m.DbColumnName == columnName));
            // 复制临时表
            string sql = $"create table {tableName}_temp as select {string.Join(",", columns.Select(m => m.DbColumnName))} from {tableName};";
            this.Context.Ado.ExecuteCommand(sql);
            // 删除旧表
            DropTable(tableName);
            // 重命名临时表
            RenameTable($"{tableName}_temp", tableName);
            return true;
        }
        private List<DbColumnInfo> GetColumnsByTableName(string tableName)
        {
            tableName = SqlBuilder.GetTranslationTableName(tableName);
            string sql = "select * from " + tableName + " limit 0,1";
            var oldIsEnableLog = this.Context.Ado.IsEnableLogEvent;
            this.Context.Ado.IsEnableLogEvent = false;
            using (DbDataReader reader = (SQLiteDataReader)this.Context.Ado.GetDataReader(sql))
            {
                this.Context.Ado.IsEnableLogEvent = oldIsEnableLog;
                List<DbColumnInfo> result = new List<DbColumnInfo>();
                var schemaTable = reader.GetSchemaTable();
                foreach (DataRow row in schemaTable.Rows)
                {
                    DbColumnInfo column = new DbColumnInfo()
                    {
                        TableName = this.SqlBuilder.GetNoTranslationColumnName(tableName + ""),
                        DataType = row["DataTypeName"].ToString().Trim(),
                        IsNullable = (bool)row["AllowDBNull"],
                        IsIdentity = (bool)row["IsAutoIncrement"],
                        ColumnDescription = null,
                        DbColumnName = row["ColumnName"].ToString(),
                        DefaultValue = row["defaultValue"].ToString(),
                        IsPrimarykey = (bool)row["IsKey"],
                        Length = Convert.ToInt32(row["ColumnSize"])
                    };
                    result.Add(column);
                }
                return result;
            }
        }
        public override bool RenameTable(string oldTableName, string newTableName)
        {
            string sql = string.Format(this.RenameTableSql, oldTableName, newTableName);
            this.Context.Ado.ExecuteCommand(sql);
            return true;
        }
        public override bool RenameColumn(string tableName, string oldColumnName, string newColumnName)
        {
            // 目前Sqlite 没有修改列功能,使用复制功能实现
            var columns = GetColumnInfosByTableName(tableName, false);
            var column = columns.FirstOrDefault(m => m.DbColumnName == oldColumnName);
            column.DbColumnName = $"{column.DbColumnName} as {newColumnName}";
            // 复制临时表
            string sql = $"create table {tableName}_temp as select {string.Join(",", columns.Select(m => m.DbColumnName))} from {tableName};";
            this.Context.Ado.ExecuteCommand(sql);
            // 删除旧表
            DropTable(tableName);
            // 重命名临时表
            RenameTable($"{tableName}_temp", tableName);
            return true;
        }
        public override bool AddPrimaryKey(string tableName, string columnName, bool IsIdentity = false)
        {
            // 目前Sqlite 没有添加主键功能,使用复制功能实现
            var columns = GetColumnInfosByTableName(tableName, false);
            // 创建临时表
            string sql = $"create table [{tableName}_temp]({string.Join(",", columns.Select(m => $"[{m.DbColumnName}] {(IsIdentity && m.DbColumnName == columnName ? "integer" : m.DataType)} {GetSize(m)} {(m.IsNullable ? m.DbColumnName == columnName ? CreateTableNotNull : this.CreateTableNull : CreateTableNotNull)}{(IsIdentity && m.DbColumnName == columnName ? " PRIMARY KEY AUTOINCREMENT" : "")}{(!string.IsNullOrWhiteSpace(m.DefaultValue) ? $" DEFAULT ({m.DefaultValue})" : null)}"))}{(!IsIdentity ? $", CONSTRAINT [sqlite_autoindex_{tableName}_1] PRIMARY KEY ([{columnName}])" : null)});";
            this.Context.Ado.ExecuteCommand(sql);
            sql = $"insert into [{tableName}_temp] select * from [{tableName}];";
            this.Context.Ado.ExecuteCommand(sql);
            // 删除旧表
            DropTable(tableName);
            // 重命名临时表
            RenameTable($"{tableName}_temp", tableName);
            return true;
        }
        public override bool BackupTable(string oldTableName, string newTableName, int maxBackupDataRows = int.MaxValue)
        {
            oldTableName = this.SqlBuilder.GetTranslationTableName(oldTableName);
            newTableName = this.SqlBuilder.GetTranslationTableName(newTableName);
            string sql = string.Format(this.BackupTableSql, newTableName, oldTableName, maxBackupDataRows);
            this.Context.Ado.ExecuteCommand(sql);
            return true;
        }
        public override bool CreateTable(string tableName, List<DbColumnInfo> columns, bool isCreatePrimaryKey = true)
        {
            if (columns.HasValue())
            {
                foreach (var item in columns)
                {
                    //if (item.DbColumnName.Equals("GUID", StringComparison.CurrentCultureIgnoreCase))
                    //{
                    //    item.Length = 20;
                    //}
                    if (item.IsIdentity && !item.IsPrimarykey)
                    {
                        item.IsPrimarykey = true;
                        item.DataType = "integer";
                        Check.Exception(item.DataType == "integer", "Identity only integer type");
                    }
                    if (item.IsIdentity)
                    {
                        // sqlite 只支持 integer 自增长
                        item.DataType = "integer";
                    }
                }
            }
            string sql = GetCreateTableSql(tableName, columns);
            string primaryKeyInfo = null;

            if (!isCreatePrimaryKey || columns.Count(it => it.IsPrimarykey) > 1)
            {
                sql = sql.Replace("PRIMARY KEY AUTOINCREMENT", "").Replace("PRIMARY KEY", "");
            }

            if (columns.Count(it => it.IsPrimarykey) > 1 && isCreatePrimaryKey)
            {
                primaryKeyInfo = string.Format(",\r\n Primary key({0})", string.Join(",", columns.Where(it => it.IsPrimarykey).Select(it => this.SqlBuilder.GetTranslationColumnName(it.DbColumnName))));
                primaryKeyInfo = primaryKeyInfo.Replace("`", "\"");
            }

            sql = sql.Replace("$PrimaryKey", primaryKeyInfo);

            this.Context.Ado.ExecuteCommand(sql);
            return true;
        }
        protected override string GetCreateTableSql(string tableName, List<DbColumnInfo> columns)
        {
            List<string> columnArray = new List<string>();
            Check.Exception(columns.IsNullOrEmpty(), "No columns found ");
            foreach (var item in columns)
            {
                string columnName = item.DbColumnName;
                string dataType = item.DataType;
                if (dataType == "varchar" && item.Length == 0)
                {
                    item.Length = 1;
                }
                string dataSize = GetSize(item);
                string nullType = item.IsNullable ? this.CreateTableNull : CreateTableNotNull;
                string primaryKey = item.IsPrimarykey ? this.CreateTablePirmaryKey : null;
                string identity = item.IsIdentity ? this.CreateTableIdentity : null;
                string addItem = string.Format(this.CreateTableColumn, this.SqlBuilder.GetTranslationColumnName(columnName), dataType, dataSize, nullType, primaryKey, identity);
                columnArray.Add(addItem);
            }
            string tableString = string.Format(this.CreateTableSql, this.SqlBuilder.GetTranslationTableName(tableName), string.Join(",\r\n", columnArray));
            tableString = tableString.Replace("`", "\"");
            return tableString;
        }
        public override bool IsAnyConstraint(string constraintName)
        {
            throw new NotSupportedException("MySql IsAnyConstraint NotSupportedException");
        }
        public override bool BackupDataBase(string databaseName, string fullFileName)
        {
            Check.ThrowNotSupportedException("MySql BackupDataBase NotSupported");
            return false;
        }
        private List<T> GetListOrCache<T>(string cacheKey, string sql)
        {
            return this.Context.Utilities.GetReflectionInoCacheInstance().GetOrCreate(cacheKey,
             () =>
             {
                 var isEnableLogEvent = this.Context.Ado.IsEnableLogEvent;
                 this.Context.Ado.IsEnableLogEvent = false;
                 var reval = this.Context.Ado.SqlQuery<T>(sql);
                 this.Context.Ado.IsEnableLogEvent = isEnableLogEvent;
                 return reval;
             });
        }
        #endregion
    }
}
