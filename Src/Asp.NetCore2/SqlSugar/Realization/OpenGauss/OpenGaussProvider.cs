﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace SqlSugar
{
    public partial class OpenGaussProvider : AdoProvider
    {
        public OpenGaussProvider() { }
        public override IDbConnection Connection
        {
            get
            {
                if (base._DbConnection == null)
                {
                    try
                    {
                        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
                        //TODO: 临时解决 加 ;No Reset On Close=true 目前没有碰到其他问题，原因 OpenGauss 数据库 不支持 DISCARD， true 会导致 ERROR: DISCARD statement is not yet supported. 错误
                        var npgsqlConnectionString = $"{base.Context.CurrentConnectionConfig.ConnectionString};No Reset On Close=true";
                        base._DbConnection = new NpgsqlConnection(npgsqlConnectionString);
                    }
                    catch (Exception ex)
                    {
                        Check.Exception(true, ErrorMessage.ConnnectionOpen, ex.Message);
                    }
                }
                return base._DbConnection;
            }
            set
            {
                base._DbConnection = value;
            }
        }

        public override void BeginTran(string transactionName)
        {
            base.BeginTran();
        }
        /// <summary>
        /// Only SqlServer
        /// </summary>
        /// <param name="iso"></param>
        /// <param name="transactionName"></param>
        public override void BeginTran(IsolationLevel iso, string transactionName)
        {
            base.BeginTran(iso);
        }
        public override IDataAdapter GetAdapter()
        {
            return new NpgsqlDataAdapter();
        }
        public override DbCommand GetCommand(string sql, SugarParameter[] parameters)
        {
            NpgsqlCommand sqlCommand = new NpgsqlCommand(sql, (NpgsqlConnection)this.Connection);
            sqlCommand.CommandType = this.CommandType;
            sqlCommand.CommandTimeout = this.CommandTimeOut;
            if (this.Transaction != null)
            {
                sqlCommand.Transaction = (NpgsqlTransaction)this.Transaction;
            }
            if (parameters.HasValue())
            {
                IDataParameter[] ipars = ToIDbDataParameter(parameters);
                sqlCommand.Parameters.AddRange((NpgsqlParameter[])ipars);
            }
            CheckConnection();
            return sqlCommand;
        }
        public override void SetCommandToAdapter(IDataAdapter dataAdapter, DbCommand command)
        {
            ((NpgsqlDataAdapter)dataAdapter).SelectCommand = (NpgsqlCommand)command;
        }
        /// <summary>
        /// if OpenGauss return NpgsqlParameter [] pars ...
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override IDataParameter[] ToIDbDataParameter(params SugarParameter[] parameters)
        {
            if (parameters == null || parameters.Length == 0) return null;
            NpgsqlParameter[] result = new NpgsqlParameter[parameters.Length];
            int index = 0;
            var isVarchar = this.Context.IsVarchar();
            foreach (var parameter in parameters)
            {
                if (parameter.Value == null) parameter.Value = DBNull.Value;
                if (parameter.Value is System.Data.SqlTypes.SqlDateTime && parameter.DbType == System.Data.DbType.AnsiString)
                {
                    parameter.DbType = System.Data.DbType.DateTime;
                    parameter.Value = DBNull.Value;
                }
                //if (parameter.DbType == System.Data.DbType.Guid)
                //{
                //    parameter.DbType = System.Data.DbType.AnsiString;
                //    parameter.Value = parameter.Value.ToString();
                //}
                var sqlParameter = new NpgsqlParameter();
                sqlParameter.ParameterName = parameter.ParameterName;
                sqlParameter.Size = parameter.Size;
                sqlParameter.Value = parameter.Value;
                sqlParameter.DbType = parameter.DbType;
                sqlParameter.Direction = parameter.Direction;
                if (parameter.IsJson)
                {
                    sqlParameter.NpgsqlDbType = NpgsqlDbType.Json;
                }
                if (parameter.IsArray)
                {
                    //    sqlParameter.Value = this.Context.Utilities.SerializeObject(sqlParameter.Value);
                    var type = sqlParameter.Value.GetType();
                    if (ArrayMapping.ContainsKey(type))
                    {
                        sqlParameter.NpgsqlDbType = ArrayMapping[type] | NpgsqlDbType.Array;
                    }
                    else if (type == DBNull.Value.GetType())
                    {
                        if (parameter.DbType.IsIn(System.Data.DbType.Int32))
                        {
                            sqlParameter.NpgsqlDbType = NpgsqlDbType.Integer | NpgsqlDbType.Array;
                        }
                        else if (parameter.DbType.IsIn(System.Data.DbType.Int64))
                        {
                            sqlParameter.NpgsqlDbType = NpgsqlDbType.Bigint | NpgsqlDbType.Array;
                        }
                        else
                        {
                            sqlParameter.NpgsqlDbType = NpgsqlDbType.Text | NpgsqlDbType.Array;
                        }

                    }
                    else
                    {
                        Check.Exception(true, sqlParameter.Value.GetType().Name + " No Support");
                    }
                }
                if (sqlParameter.Direction == 0)
                {
                    sqlParameter.Direction = ParameterDirection.Input;
                }
                result[index] = sqlParameter;
                if (sqlParameter.Direction.IsIn(ParameterDirection.Output, ParameterDirection.InputOutput, ParameterDirection.ReturnValue))
                {
                    if (this.OutputParameters == null) this.OutputParameters = new List<IDataParameter>();
                    this.OutputParameters.RemoveAll(it => it.ParameterName == sqlParameter.ParameterName);
                    this.OutputParameters.Add(sqlParameter);
                }
                if (isVarchar && sqlParameter.DbType == System.Data.DbType.String)
                {
                    sqlParameter.DbType = System.Data.DbType.AnsiString;
                }
                ++index;
            }
            return result;
        }


        static readonly Dictionary<Type, NpgsqlDbType> ArrayMapping = new Dictionary<Type, NpgsqlDbType>()
        {
            { typeof(int[]),NpgsqlDbType.Integer},
            { typeof(short[]),NpgsqlDbType.Smallint},
            { typeof(long[]),NpgsqlDbType.Bigint},
            { typeof(decimal[]),NpgsqlDbType.Numeric},
            { typeof(char[]),NpgsqlDbType.Text},
            { typeof(byte[]),NpgsqlDbType.Bytea},
            { typeof(bool[]),NpgsqlDbType.Boolean},
            { typeof(DateTime[]),NpgsqlDbType.Date},
            { typeof(float[]),NpgsqlDbType.Real},
            { typeof(Guid[]),NpgsqlDbType.Varchar },

            { typeof(int?[]),NpgsqlDbType.Integer},
            { typeof(short?[]),NpgsqlDbType.Smallint},
            { typeof(long?[]),NpgsqlDbType.Bigint},
            { typeof(decimal?[]),NpgsqlDbType.Numeric},
            { typeof(char?[]),NpgsqlDbType.Text},
            { typeof(byte?[]),NpgsqlDbType.Bytea},
            { typeof(bool?[]),NpgsqlDbType.Boolean},
            { typeof(DateTime?[]),NpgsqlDbType.Date},


            { typeof(string[]), NpgsqlDbType.Text},
            { typeof(float?[]),NpgsqlDbType.Real},
        };
    }
}