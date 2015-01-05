// created on 18/5/2002 at 01:25

// Npgsql.NpgsqlParameter.cs
//
// Author:
//    Francisco Jr. (fxjrlists@yahoo.com.br)
//
//    Copyright (C) 2002 The Npgsql Development Team
//    npgsql-general@gborg.postgresql.org
//    http://gborg.postgresql.org/project/npgsql/projdisplay.php
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose, without fee, and without a written
// agreement is hereby granted, provided that the above copyright notice
// and this paragraph and the following two paragraphs appear in all copies.
//
// IN NO EVENT SHALL THE NPGSQL DEVELOPMENT TEAM BE LIABLE TO ANY PARTY
// FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
// INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS
// DOCUMENTATION, EVEN IF THE NPGSQL DEVELOPMENT TEAM HAS BEEN ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//
// THE NPGSQL DEVELOPMENT TEAM SPECIFICALLY DISCLAIMS ANY WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS
// ON AN "AS IS" BASIS, AND THE NPGSQL DEVELOPMENT TEAM HAS NO OBLIGATIONS
// TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Resources;
using Npgsql.Localization;
using NpgsqlTypes;

#if WITHDESIGN
using Npgsql.Design;
#endif

namespace Npgsql
{
    ///<summary>
    /// This class represents a parameter to a command that will be sent to server
    ///</summary>
#if WITHDESIGN
    [TypeConverter(typeof(NpgsqlParameterConverter))]
#endif

    public sealed class NpgsqlParameter : DbParameter, ICloneable
    {
        // Fields to implement IDbDataParameter interface.
        private byte precision = 0;
        private byte scale = 0;
        private Int32 size = 0;

        // Fields to implement IDataParameter
        private NpgsqlDbType?              npgsqldb_type;
        private DbType?                    db_type;
        private ParameterDirection direction = ParameterDirection.Input;
        private Boolean is_nullable = false;
        private String m_Name = String.Empty;
        private String source_column = String.Empty;
        private DataRowVersion source_version = DataRowVersion.Current;
        private Object value = null;
        private Object npgsqlValue = null;
        private Boolean sourceColumnNullMapping;
        private NpgsqlParameterCollection collection = null;

        internal bool IsBound { get; private set; }
        internal TypeHandler BoundHandler { get; private set; }
        internal FormatCode BoundFormatCode { get; private set; }
        /// <summary>
        /// The number of bytes the bound value of this parameter requires
        /// </summary>
        internal int BoundSize { get; private set; }
        internal uint TypeOID { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see> class.
        /// </summary>
        public NpgsqlParameter()
        {
            //type_info = NpgsqlTypesHelper.GetNativeTypeInfo(typeof(String));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>
        /// class with the parameter m_Name and a value of the new <b>NpgsqlParameter</b>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="value">An <see cref="System.Object">Object</see> that is the value of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.</param>
        /// <remarks>
        /// <p>When you specify an <see cref="System.Object">Object</see>
        /// in the value parameter, the <see cref="System.Data.DbType">DbType</see> is
        /// inferred from the .NET Framework type of the <b>Object</b>.</p>
        /// <p>When using this constructor, you must be aware of a possible misuse of the constructor which takes a DbType parameter.
        /// This happens when calling this constructor passing an int 0 and the compiler thinks you are passing a value of DbType.
        /// Use <code> Convert.ToInt32(value) </code> for example to have compiler calling the correct constructor.</p>
        /// </remarks>
        public NpgsqlParameter(String parameterName, object value)
        {
            this.ParameterName = parameterName;
            this.Value = value;
        }

        /// <summary>
        /// The collection to which this parameter belongs, if any.
        /// </summary>
        public NpgsqlParameterCollection Collection
        {
            get { return collection; }

            internal set
            {
                collection = value;
                ClearBind();
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>
        /// class with the parameter m_Name and the data type.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="System.Data.DbType">DbType</see> values.</param>
        public NpgsqlParameter(String parameterName, NpgsqlDbType parameterType)
            : this(parameterName, parameterType, 0, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="System.Data.DbType">DbType</see> values.</param>
        public NpgsqlParameter(String parameterName, DbType parameterType)
            : this(parameterName, NpgsqlTypesHelper.GetNativeTypeInfo(parameterType).NpgsqlDbType, 0, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="NpgsqlTypes.NpgsqlDbType">NpgsqlDbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        public NpgsqlParameter(String parameterName, NpgsqlDbType parameterType, Int32 size)
            : this(parameterName, parameterType, size, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="System.Data.DbType">DbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        public NpgsqlParameter(String parameterName, DbType parameterType, Int32 size)
            : this(parameterName, NpgsqlTypesHelper.GetNativeTypeInfo(parameterType).NpgsqlDbType, size, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="NpgsqlTypes.NpgsqlDbType">NpgsqlDbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        /// <param m_Name="sourceColumn">The m_Name of the source column.</param>
        public NpgsqlParameter(String parameterName, NpgsqlDbType parameterType, Int32 size, String sourceColumn)
        {
            this.ParameterName = parameterName;

            NpgsqlDbType = parameterType; //Allow the setter to catch any exceptions.

            this.size = size;
            source_column = sourceColumn;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="System.Data.DbType">DbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        /// <param m_Name="sourceColumn">The m_Name of the source column.</param>
        public NpgsqlParameter(String parameterName, DbType parameterType, Int32 size, String sourceColumn)
            : this(parameterName, NpgsqlTypesHelper.GetNativeTypeInfo(parameterType).NpgsqlDbType, size, sourceColumn)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="NpgsqlTypes.NpgsqlDbType">NpgsqlDbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        /// <param m_Name="sourceColumn">The m_Name of the source column.</param>
        /// <param m_Name="direction">One of the <see cref="System.Data.ParameterDirection">ParameterDirection</see> values.</param>
        /// <param m_Name="isNullable"><b>true</b> if the value of the field can be null, otherwise <b>false</b>.</param>
        /// <param m_Name="precision">The total number of digits to the left and right of the decimal point to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved.</param>
        /// <param m_Name="scale">The total number of decimal places to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved.</param>
        /// <param m_Name="sourceVersion">One of the <see cref="System.Data.DataRowVersion">DataRowVersion</see> values.</param>
        /// <param m_Name="value">An <see cref="System.Object">Object</see> that is the value
        /// of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.</param>
        public NpgsqlParameter(String parameterName, NpgsqlDbType parameterType, Int32 size, String sourceColumn,
                               ParameterDirection direction, bool isNullable, byte precision, byte scale,
                               DataRowVersion sourceVersion, object value)
        {
            this.ParameterName = parameterName;
            this.Size = size;
            this.SourceColumn = sourceColumn;
            this.Direction = direction;
            this.IsNullable = isNullable;
            this.Precision = precision;
            this.Scale = scale;
            this.SourceVersion = sourceVersion;
            this.Value = value;
            
            NpgsqlDbType = parameterType; //allow the setter to catch exceptions if necessary.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <param m_Name="parameterName">The m_Name of the parameter to map.</param>
        /// <param m_Name="parameterType">One of the <see cref="System.Data.DbType">DbType</see> values.</param>
        /// <param m_Name="size">The length of the parameter.</param>
        /// <param m_Name="sourceColumn">The m_Name of the source column.</param>
        /// <param m_Name="direction">One of the <see cref="System.Data.ParameterDirection">ParameterDirection</see> values.</param>
        /// <param m_Name="isNullable"><b>true</b> if the value of the field can be null, otherwise <b>false</b>.</param>
        /// <param m_Name="precision">The total number of digits to the left and right of the decimal point to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved.</param>
        /// <param m_Name="scale">The total number of decimal places to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved.</param>
        /// <param m_Name="sourceVersion">One of the <see cref="System.Data.DataRowVersion">DataRowVersion</see> values.</param>
        /// <param m_Name="value">An <see cref="System.Object">Object</see> that is the value
        /// of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.</param>
        public NpgsqlParameter(String parameterName, DbType parameterType, Int32 size, String sourceColumn,
                               ParameterDirection direction, bool isNullable, byte precision, byte scale,
                               DataRowVersion sourceVersion, object value)
            : this(
                parameterName, NpgsqlTypesHelper.GetNativeTypeInfo(parameterType).NpgsqlDbType, size, sourceColumn, direction,
                isNullable, precision, scale, sourceVersion, value)
        {
        }

        // Implementation of IDbDataParameter
        /// <summary>
        /// Gets or sets the maximum number of digits used to represent the
        /// <see cref="NpgsqlParameter.Value">Value</see> property.
        /// </summary>
        /// <value>The maximum number of digits used to represent the
        /// <see cref="NpgsqlParameter.Value">Value</see> property.
        /// The default value is 0, which indicates that the data provider
        /// sets the precision for <b>Value</b>.</value>
        [Category("Data"), DefaultValue((Byte)0)]
        public Byte Precision
        {
            get { return precision; }
            set
            {
                precision = value;
                ClearBind();
            }
        }

        /// <summary>
        /// Whether to use an explicit cast when included in a query.
        /// </summary>
        public Boolean UseCast
        {
            get
            {
                return npgsqldb_type != NpgsqlDbType.Unknown;

                /*
                // Prevents casts to be added for null values when they aren't needed.
                if (!useCast && (value == DBNull.Value || value == null))
                    return false;
                //return useCast; //&& (value != DBNull.Value);
                // This check for Datetime.minvalue and maxvalue is needed in order to
                // workaround a problem when comparing date values with infinity.
                // This is a known issue with postgresql and it is reported here:
                // http://archives.postgresql.org/pgsql-general/2008-10/msg00535.php
                // Josh's solution to add cast is documented here:
                // http://pgfoundry.org/forum/message.php?msg_id=1004118

                return useCast || DateTime.MinValue.Equals(value) || DateTime.MaxValue.Equals(value) || !NpgsqlTypesHelper.DefinedType(Value);
                */
            }
        }

        /// <summary>
        /// Gets or sets the number of decimal places to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved.
        /// </summary>
        /// <value>The number of decimal places to which
        /// <see cref="NpgsqlParameter.Value">Value</see> is resolved. The default is 0.</value>
        [Category("Data"), DefaultValue((Byte)0)]
        public Byte Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                ClearBind();
            }
        }

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of the data within the column.
        /// </summary>
        /// <value>The maximum size, in bytes, of the data within the column.
        /// The default value is inferred from the parameter value.</value>
        [Category("Data"), DefaultValue(0)]
        public override Int32 Size
        {
            get { return size; }
            set
            {
                size = value;
                ClearBind();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Data.DbType">DbType</see> of the parameter.
        /// </summary>
        /// <value>One of the <see cref="System.Data.DbType">DbType</see> values. The default is <b>Object</b>.</value>
        [Category("Data"), RefreshProperties(RefreshProperties.All), DefaultValue(DbType.Object)]
        public override DbType DbType
        {
            get
            {
                if (db_type.HasValue)
                    return db_type.Value;
                if (npgsqldb_type.HasValue)
                    return TypeHandlerRegistry.GetDbTypeFromNpgsqlDbType(npgsqldb_type.Value);

                if (IsNull)
                    return DbType.Object;

                return TypeHandlerRegistry.GetDbTypeFromNpgsqlDbType(TypeHandlerRegistry.InferNpgsqlDbTypeFromValue(value));
            }
            set
            {
                ClearBind();
                npgsqldb_type = TypeHandlerRegistry.GetNpgsqlDbTypeFromDbType(value);
                db_type = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="NpgsqlTypes.NpgsqlDbType">NpgsqlDbType</see> of the parameter.
        /// </summary>
        /// <value>One of the <see cref="NpgsqlTypes.NpgsqlDbType">NpgsqlDbType</see> values. The default is <b>Unknown</b>.</value>
        [Category("Data"), RefreshProperties(RefreshProperties.All), DefaultValue(NpgsqlDbType.Unknown)]
        public NpgsqlDbType NpgsqlDbType
        {
            get
            {
                if (npgsqldb_type.HasValue)
                    return npgsqldb_type.Value;

                if (IsNull)
                    return NpgsqlDbType.Unknown;

                return TypeHandlerRegistry.InferNpgsqlDbTypeFromValue(value);
            }
            set
            {
                ClearBind(); 
                if (value == NpgsqlDbType.Array)
                {
                    throw new ArgumentOutOfRangeException("value", L10N.ParameterTypeIsOnlyArray);
                }
                if (value == NpgsqlDbType.Range)
                {
                    throw new ArgumentOutOfRangeException("value", "Cannot set NpgsqlDbType to just Range, Binary-Or with the element type (e.g. Range of integer is NpgsqlDbType.Range | NpgsqlDbType.Integer)");
                }
                npgsqldb_type = value;
            }
        }

        /*internal NpgsqlNativeTypeInfo TypeInfo
        {
            get
            {
                if (type_info == null)
                {
                    //type_info = NpgsqlTypesHelper.GetNativeTypeInfo(typeof(String));
                    return defaultTypeInfo;
                }
                return type_info;
            }
        }*/

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is input-only,
        /// output-only, bidirectional, or a stored procedure return value parameter.
        /// </summary>
        /// <value>One of the <see cref="System.Data.ParameterDirection">ParameterDirection</see>
        /// values. The default is <b>Input</b>.</value>
        [Category("Data"), DefaultValue(ParameterDirection.Input)]
        public override ParameterDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter accepts null values.
        /// </summary>
        /// <value><b>true</b> if null values are accepted; otherwise, <b>false</b>. The default is <b>false</b>.</value>

#if WITHDESIGN
        [EditorBrowsable(EditorBrowsableState.Advanced), Browsable(false), DefaultValue(false), DesignOnly(true)]
#endif

        public override Boolean IsNullable
        {
            get { return is_nullable; }
            set { is_nullable = value; }
        }

        /// <summary>
        /// Gets or sets the m_Name of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// </summary>
        /// <value>The m_Name of the <see cref="NpgsqlParameter">NpgsqlParameter</see>.
        /// The default is an empty string.</value>
        [DefaultValue("")]
        public override String ParameterName
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                if (value == null)
                {
                    m_Name = String.Empty;
                }
                // no longer prefix with : so that the m_Name returned is the m_Name set

                m_Name = m_Name.Trim();

                if (collection != null)
                {
                    collection.InvalidateHashLookups();
                    ClearBind();
                }
            }
        }

        /// <summary>
        /// The m_Name scrubbed of any optional marker
        /// </summary>
        internal string CleanName
        {
            get
            {
                string name = ParameterName;
                if (name[0] == ':' || name[0] == '@')
                {
                    return name.Length > 1 ? name.Substring(1) : string.Empty;
                }
                return name;

            }
        }

        /// <summary>
        /// Gets or sets the m_Name of the source column that is mapped to the
        /// <see cref="System.Data.DataSet">DataSet</see> and used for loading or
        /// returning the <see cref="NpgsqlParameter.Value">Value</see>.
        /// </summary>
        /// <value>The m_Name of the source column that is mapped to the
        /// <see cref="System.Data.DataSet">DataSet</see>. The default is an empty string.</value>
        [Category("Data"), DefaultValue("")]
        public override String SourceColumn
        {
            get { return source_column; }
            set { source_column = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Data.DataRowVersion">DataRowVersion</see>
        /// to use when loading <see cref="NpgsqlParameter.Value">Value</see>.
        /// </summary>
        /// <value>One of the <see cref="System.Data.DataRowVersion">DataRowVersion</see> values.
        /// The default is <b>Current</b>.</value>
        [Category("Data"), DefaultValue(DataRowVersion.Current)]
        public override DataRowVersion SourceVersion
        {
            get { return source_version; }
            set { source_version = value; }
        }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        /// <value>An <see cref="System.Object">Object</see> that is the value of the parameter.
        /// The default value is null.</value>
        [TypeConverter(typeof(StringConverter)), Category("Data")]
        public override Object Value
        {
            get
            {

                return this.value;

                /*
                NpgsqlEventLog.LogPropertyGet(LogLevel.Normal, CLASSNAME, "Value");
                //return value;

                NpgsqlBackendTypeInfo backendTypeInfo;

                if (NpgsqlTypesHelper.TryGetBackendTypeInfo(type_info.Name, out backendTypeInfo))
                {
                    return backendTypeInfo.ConvertToFrameworkType(NpgsqlValue);
                }

                throw new NotSupportedException();
                */

            } // [TODO] Check and validate data type.
            set
            {
                this.value = value;
                this.npgsqlValue = value;

                ClearBind();

                return;
            }
        }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        /// <value>An <see cref="System.Object">Object</see> that is the value of the parameter.
        /// The default value is null.</value>
        [TypeConverter(typeof(StringConverter)), Category("Data")]
        public Object NpgsqlValue
        {
            get { return npgsqlValue ?? value; }
            set {
                Value = value;
                ClearBind();
            }
        }

        internal bool IsNull
        {
            get { return value == null || value is DBNull; }
        }

        internal void Bind(TypeHandlerRegistry registry)
        {
            // TODO: Make sure we do full validation here, no binding exception can occur after this method
            // returns successfully
            var handlerOID = npgsqldb_type.HasValue
                ? registry.GetOidFromNpgsqlDbType(npgsqldb_type.Value) // Todo: enum/composite types
                : registry.InferTypeOidFromValue(value);            

            BoundHandler = registry[handlerOID];

            BoundFormatCode = BoundHandler.PreferTextWrite || !BoundHandler.SupportsBinaryWrite
                ? FormatCode.Text
                : FormatCode.Binary;

            if (BoundFormatCode == FormatCode.Text)
                throw new NotImplementedException("Text binding");

            BoundSize = BoundHandler.BinarySize(Value);

            IsBound = true;
        }

        void ClearBind()
        {
            IsBound = false;
            BoundHandler = null;
        }

        /// <summary>
        /// Reset DBType.
        /// </summary>
        public override void ResetDbType()
        {
            //type_info = NpgsqlTypesHelper.GetNativeTypeInfo(typeof(String));
            db_type = null;
            npgsqldb_type = null;
            this.Value = Value;
            ClearBind();
        }

        internal void SetTypeOID(uint oid, NativeToBackendTypeConverterOptions nativeToBackendTypeConverterOptions)
        {
            TypeOID = oid;
        }

        /// <summary>
        /// Source column mapping.
        /// </summary>
        public override bool SourceColumnNullMapping
        {
            get { return sourceColumnNullMapping; }
            set { sourceColumnNullMapping = value; }
        }

        internal bool IsInputDirection
        {
            get { return direction == ParameterDirection.InputOutput || direction == ParameterDirection.Input; }
        }

        internal bool IsOutputDirection
        {
            get { return direction == ParameterDirection.InputOutput || direction == ParameterDirection.Output; }
        }

        /// <summary>
        /// Creates a new <see cref="NpgsqlParameter">NpgsqlParameter</see> that
        /// is a copy of the current instance.
        /// </summary>
        /// <returns>A new <see cref="NpgsqlParameter">NpgsqlParameter</see> that is a copy of this instance.</returns>
        public NpgsqlParameter Clone()
        {
            // use fields instead of properties
            // to avoid auto-initializing something like type_info
            NpgsqlParameter clone = new NpgsqlParameter();
            clone.precision = precision;
            clone.scale = scale;
            clone.size = size;
            clone.db_type = db_type;
            clone.npgsqldb_type = npgsqldb_type;
            clone.direction = direction;
            clone.is_nullable = is_nullable;
            clone.m_Name = m_Name;
            clone.source_column = source_column;
            clone.source_version = source_version;
            clone.value = value;
            clone.npgsqlValue = npgsqlValue;
            clone.sourceColumnNullMapping = sourceColumnNullMapping;

            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
