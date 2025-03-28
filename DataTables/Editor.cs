﻿// <copyright>Copyright (c) 2014-2017 SpryMedia Ltd - All Rights Reserved</copyright>
//
// <summary>
// Editor class for reading tables as well as creating, editing and deleting rows
// </summary>
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using DataTables.EditorUtil;

namespace DataTables
{
    /// <summary>
    /// DataTables Editor base class for creating editable tables.
    ///
    /// Editor class instances are capable of servicing all of the requests that
    /// DataTables and Editor will make from the client-side - specifically:
    ///
    /// * Get data
    /// * Create new record
    /// * Edit existing record
    /// * Delete existing records
    ///
    /// The Editor instance is configured with information regarding the
    /// database table fields that you which to make editable, and other information
    /// needed to read and write to the database (table name for example!).
    ///
    /// This documentation is very much focused on describing the API presented
    /// by these DataTables Editor classes. For a more general overview of how
    /// the Editor class is used, and how to install Editor on your server, please
    /// refer to the Editor manual ( https://editor.datatables.net/manual ).
    /// </summary>
    public class Editor
    {
        /// <summary>
        /// Get the form action.
        /// </summary>
        /// <param name="request">Data sent from the client-side</param>
        /// <returns>Request type</returns>
        public static DtRequest.RequestTypes Action(DtRequest request)
        {
            return request.RequestType;
        }

        /// <summary>
        /// Get the form action. For use with an HttpRequest object
        /// </summary>
        /// <param name="http">Data sent from the client-side</param>
        /// <returns>Request type</returns>
        public static DtRequest.RequestTypes Action(HttpRequest http)
        {
            return Action(http.Form);
        }

        /// <summary>
        /// Get the form action. For use with MVC's 'Request.Form' collection
        /// </summary>
        /// <param name="data">Data sent from the client-side</param>
        /// <returns>Request type</returns>
        public static DtRequest.RequestTypes Action(NameValueCollection data = null)
        {
            var list = new List<KeyValuePair<string, string>>();

            if (data != null)
            {
                foreach (var key in data.AllKeys)
                {
                    list.Add(new KeyValuePair<string, string>(key, data[key]));
                }
            }

            var request = new DtRequest(list);
            return request.RequestType;
        }

        /// <summary>
        /// Get the form action. For use with WebAPI's 'FormDataCollection' collection
        /// </summary>
        /// <param name="data">Data sent from the client-side</param>
        /// <returns>Request type</returns>
        public static DtRequest.RequestTypes Action(IEnumerable<KeyValuePair<string, string>> data = null)
        {
            var request = new DtRequest(data);
            return request.RequestType;
        }

        /// <summary>
        /// Version string
        /// </summary>
        public const string Version = "1.7.3";

        /// <summary>
        /// Create a new Editor instance
        /// </summary>
        /// <param name="db">An instance of the DataTables Database class that we can use for the DB connection. Can also be set with the <code>Db()</code> method.</param>
        /// <param name="table">The table name in the database to read and write information from and to. Can also be set with the <code>Table()</code> method.</param>
        /// <param name="pkey">Primary key column name in the table given. Can also be set with the <code>PKey()</code> method.</param>
        public Editor(Database db = null, string table = null, string pkey = null)
        {
            if (db != null)
            {
                Db(db);
            }

            if (table != null)
            {
                Table(table);
            }

            if (pkey != null)
            {
                Pkey(pkey);
            }
        }

        /// <summary>
        /// Create a new Editor instance
        /// </summary>
        /// <param name="db">An instance of the DataTables Database class that we can use for the DB connection. Can also be set with the <code>Db()</code> method.</param>
        /// <param name="table">The table name in the database to read and write information from and to. Can also be set with the <code>Table()</code> method.</param>
        /// <param name="pkey">Primary key column names in the table given. Can also be set with the <code>PKey()</code> method.</param>
        public Editor(Database db, string table, string[] pkey)
        {
            if (db != null)
            {
                Db(db);
            }

            if (table != null)
            {
                Table(table);
            }

            if (pkey != null)
            {
                Pkey(pkey);
            }
        }


        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Public events
         */

        /// <summary>
        /// Event which is triggered immediately prior to a row being created.
        /// Note that for multi-row creation it is triggered for each row
        /// indivudally.
        /// </summary>
        public event EventHandler<PreGetEventArgs> PreGet;

        /// <summary>
        /// Event which is triggered immediately after a row has been created.
        /// Note that for multi-row creation it is triggered for each row
        /// indivudally.
        /// </summary>
        public event EventHandler<PostGetEventArgs> PostGet;

        /// <summary>
        /// Event which is triggered immediately prior to a row being created.
        /// Note that for multi-row creation it is triggered for each row
        /// indivudally.
        /// </summary>
        public event EventHandler<PreCreateEventArgs> PreCreate;

        /// <summary>
        /// Event which is triggered after the new row has been written to the
        /// database, but before it is then read back, allowing the row to be
        /// manipulated externally. Note that for multi-row creation it is
        /// triggered for each row indivudally.
        /// </summary>
        public event EventHandler<WriteCreateEventArgs> WriteCreate;

        /// <summary>
        /// Event which is triggered immediately after a row has been created.
        /// Note that for multi-row creation it is triggered for each row
        /// indivudally.
        /// </summary>
        public event EventHandler<PostCreateEventArgs> PostCreate;

        /// <summary>
        /// Event which is triggered immediately prior to a row being edited.
        /// Note that for multi-row editing, it is triggered for each row
        /// individually.
        /// </summary>
        public event EventHandler<PreEditEventArgs> PreEdit;

        /// <summary>
        /// Event which is triggered after the row has been updated on the
        /// database, but before it is then read back, allowing the row to be
        /// manipulated externally. Note that for multi-row creation it is
        /// triggered for each row indivudally.
        /// individually.
        /// </summary>
        public event EventHandler<WriteEditEventArgs> WriteEdit;

        /// <summary>
        /// Event which is triggered immediately after a row being edited.
        /// Note that for multi-row editing, it is triggered for each row
        /// individually.
        /// </summary>
        public event EventHandler<PostEditEventArgs> PostEdit;

        /// <summary>
        /// Event which is triggered immediately prior to a row being deleted.
        /// Note that for multi-row deletion, it is triggered for each row
        /// individually.
        /// </summary>
        public event EventHandler<PreRemoveEventArgs> PreRemove;

        /// <summary>
        /// Event which is triggered immediately after a row being deleted.
        /// Note that for multi-row deletion, it is triggered for each row
        /// individually.
        /// </summary>
        public event EventHandler<PostRemoveEventArgs> PostRemove;

        /// <summary>
        /// Event which is triggered immediately prior to an uploaded file
        /// being processed.
        /// </summary>
        public event EventHandler<PreUploadEventArgs> PreUpload;

        /// <summary>
        /// Event which is triggered immediately after an uploaded file has
        /// been processed.
        /// </summary>
        public event EventHandler<PostUploadEventArgs> PostUpload;


        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Private parameters
         */

        private Database _db;
        private readonly List<Field> _field = new List<Field>();
        private string _idPrefix = "row_";
        private DtRequest _processData;
        private Dictionary<string, object> _formData;
        private Dictionary<string, Type> _userModelT = new Dictionary<string, Type>();
        private string[] _pkey = { "id" };
        private readonly List<string> _table = new List<string>();
        private bool _transaction = true;
        private readonly List<WhereCondition> _where = new List<WhereCondition>();
        private readonly List<LeftJoin> _leftJoin = new List<LeftJoin>();
        private DtResponse _out;
        private readonly List<MJoin> _mJoin = new List<MJoin>();
        private HttpRequest _request;
        private HttpFileCollection _requestFiles;
        private readonly Dictionary<string, List<Delegate>> _events = new Dictionary<string, List<Delegate>>();
        private bool _tryCatch = true;
        private bool _debug = false;
        private List<object> _DebugInfo = new List<object>();
        private Func<Editor, DtRequest.RequestTypes, DtRequest, string> _validator;

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Public methods
         */

        /// <summary>
        /// Get the response object that has been created by this instance. This
        /// is only useful after <code>process()</code> has been called.
        /// </summary>
        /// <returns>The response object as populated by this instance</returns>
        public DtResponse Data()
        {
            return _out;
        }

        /// <summary>
        /// Get the database instance used by this instance
        /// </summary>
        /// <returns>Database connection instance</returns>
        public Database Db()
        {
            return _db;
        }

        /// <summary>
        /// Set the database connection instance
        /// </summary>
        /// <param name="db">Connection instance to set</param>
        /// <returns>Self for chaining</returns>
        public Editor Db(Database db)
        {
            _db = db;
            return this;
        }

        /// <summary>
        /// Get the debug state
        /// </summary>
        /// <returns>true if debugging is enabled</returns>
        public bool Debug()
        {
            return _debug;
        }

        /// <summary>
        /// Set the debug state. If enabled (`true`) Editor will record information
        /// about the SQL queries it makes and return that information in the JSON
        /// sent to the client-side query once the request has been processed.
        /// </summary>
        /// <param name="debug">Flag to for how to set the debug state</param>
        /// <returns>Self for chaining</returns>
        public Editor Debug(bool debug)
        {
            _debug = debug;
            return this;
        }

        /// <summary>
        /// Add debug information to the data sent to the client-side.
        /// </summary>
        /// <param name="debug">Debug information to send</param>
        /// <returns></returns>
        public Editor Debug(string debug)
        {
            _DebugInfo.Add(debug);
            return this;
        }

        /// <summary>
        /// Add debug information to the data sent to the client-side.
        /// </summary>
        /// <param name="debug">Debug information to send</param>
        /// <returns></returns>
        public Editor Debug(object debug)
        {
            _DebugInfo.Add(debug);
            return this;
        }

        /// <summary>
        /// Get the fields that have been configured for this instance
        /// </summary>
        /// <returns>List of fields</returns>
        public List<Field> Field()
        {
            return _field;
        }

        /// <summary>
        /// Get a field instance that has already been added
        /// </summary>
        /// <param name="f">Field name to select</param>
        /// <returns>Field instance</returns>
        public Field Field(string f)
        {
            for (var i = 0; i < _field.Count(); i++)
            {
                if (_field[i].Name() == f)
                {
                    return _field[i];
                }
            }

            throw new Exception("Unknown field: " + f);
        }

        /// <summary>
        /// Add a new field to this instance
        /// </summary>
        /// <param name="f">New field to add</param>
        /// <returns>Self for chaining</returns>
        public Editor Field(Field f)
        {
            _field.Add(f);
            return this;
        }

        /// <summary>
        /// Add multiple fields too this instance
        /// </summary>
        /// <param name="fields">Collection of fields to add</param>
        /// <returns>Self for chaining</returns>
        public Editor Field(IEnumerable<Field> fields)
        {
            foreach (var f in fields)
            {
                _field.Add(f);
            }
            return this;
        }

        /// <summary>
        /// Get the DOM prefix.
        /// 
        /// Typically primary keys are numeric and this is not a valid ID value in an
        /// HTML document - is also increases the likelihood of an ID clash if multiple
        /// tables are used on a single page. As such, a prefix is assigned to the 
        /// primary key value for each row, and this is used as the DOM ID, so Editor
        /// can track individual rows.
        /// </summary>
        /// <returns>DOM prefix</returns>
        public string IdPrefix()
        {
            return _idPrefix;
        }

        /// <summary>
        /// Set the DOM prefix.
        /// 
        /// Typically primary keys are numeric and this is not a valid ID value in an
        /// HTML document - is also increases the likelihood of an ID clash if multiple
        /// tables are used on a single page. As such, a prefix is assigned to the 
        /// primary key value for each row, and this is used as the DOM ID, so Editor
        /// can track individual rows.
        /// </summary>
        /// <param name="prefix">Prefix to set</param>
        /// <returns>Self for chaining</returns>
        public Editor IdPrefix(string prefix)
        {
            _idPrefix = prefix;
            return this;
        }

        /// <summary>
        /// Get the data that is being processed by the Editor instance. This is only
        /// useful once the <code>Process()</code> method has been called, and
        /// is available for use in validation and formatter methods.
        /// </summary>
        /// <returns>Data given to <code>Process()</code></returns>
        public DtRequest InData()
        {
            return _processData;
        }

        /// <summary>
        /// Add a left join condition to the Editor instance, allowing it to operate
        /// over multiple tables. Multiple <code>leftJoin()</code> calls can be made for a
        /// single Editor instance to join multiple tables.
        ///
        /// A left join is the most common type of join that is used with Editor
        /// so this method is provided to make its use very easy to configure. Its
        /// parameters are basically the same as writing an SQL left join statement,
        /// but in this case Editor will handle the create, update and remove
        /// requirements of the join for you:
        ///
        /// * Create - On create Editor will insert the data into the primary table
        ///   and then into the joined tables - selecting the required data for each
        ///   table.
        /// * Edit - On edit Editor will update the main table, and then either
        ///   update the existing rows in the joined table that match the join and
        ///   edit conditions, or insert a new row into the joined table if required.
        /// * Remove - On delete Editor will remove the main row and then loop over
        ///   each of the joined tables and remove the joined data matching the join
        ///   link from the main table.
        ///
        /// Please note that when using join tables, Editor requires that you fully
        /// qualify each field with the field's table name. SQL can result table
        /// names for ambiguous field names, but for Editor to provide its full CRUD
        /// options, the table name must also be given. For example the field
        /// <code>first_name</code> in the table <code>users</code> would be given
        /// as <code>users.first_name</code>.
        /// </summary>
        /// <param name="table">Table name to do a join onto</param>
        /// <param name="field1">Field from the parent table to use as the join link</param>
        /// <param name="op">Join condition (`=`, '&lt;`, etc)</param>
        /// <param name="field2">Field from the child table to use as the join link</param>
        /// <returns>Self for chaining</returns>
        public Editor LeftJoin(string table, string field1, string op, string field2)
        {
            _leftJoin.Add(new LeftJoin(table, field1, op, field2));

            return this;
        }

        /// <summary>
        /// Add a 1-to-many ("mjoin") join to the Editor instance. The way the
        /// join operates is defined by the MJoin class
        /// </summary>
        /// <param name="join">MJoin link to use</param>
        /// <returns>Self for chaining</returns>
        public Editor MJoin(MJoin join)
        {
            _mJoin.Add(join);

            return this;
        }

        /// <summary>
        /// Set a model to use.
        ///
        /// In keeping with the MVC style of coding, you can define the fields
        /// and their types that you wish to get from the database in a simple
        /// class. Editor will automatically add fields from the model.
        ///
        /// Note that fields that are defined in the model can also be defined
        /// as <code>Field</code> instances should you wish to add additional
        /// options to a specific field such as formatters or validation.
        /// </summary>
        /// <typeparam name="T">Model to use</typeparam>
        /// <returns>Self for chaining</returns>
        public Editor Model<T>()
        {
            _userModelT.Add("", typeof(T));

            return this;
        }

        /// <summary>
        /// Set a model to use.
        /// </summary>
        /// <typeparam name="T">Model to use</typeparam>
        /// <returns>Self for chaining</returns>
        public Editor Model<T>(string tableName)
        {
            _userModelT.Add(tableName, typeof(T));

            return this;
        }

        /// <summary>
        /// Add an event listener. The `Editor` class will trigger an number of
        /// events that some action can be taken on.
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="callback">
        /// Callback function to execute when the event occurs
        /// </param>
        /// <returns>Self for chaining</returns>
        public Editor On(string name, Delegate callback)
        {
            if (!_events.ContainsKey(name))
            {
                _events.Add(name, new List<Delegate>());
            }

            _events[name].Add(callback);

            return this;
        }


        /// <summary>
        /// Get the database table name this Editor instance will use
        /// </summary>
        /// <returns>Table name</returns>
        public List<string> Table()
        {
            return _table;
        }

        /// <summary>
        /// Set the database table name this Editor instance will use
        /// </summary>
        /// <param name="t">Table name</param>
        /// <returns>Self for chaining</returns>
        public Editor Table(string t)
        {
            _table.Add(t);
            return this;
        }

        /// <summary>
        /// Add multiple tables to the Editor instance
        /// </summary>
        /// <param name="tables">Collection of tables to add</param>
        /// <returns>Self for chaining</returns>
        public Editor Table(IEnumerable<string> tables)
        {
            foreach (var t in tables)
            {
                _table.Add(t);
            }
            return this;
        }

        /// <summary>
        /// Get the transaction state for this instance.
        /// 
        /// When enabled (which it is by default) Editor will use an SQL transaction
        /// to ensure data integrity while it is performing operations on the table.
        /// This can be optionally disabled using this method, if required by your
        /// database configuration.
        /// </summary>
        /// <returns>The current transaction value/state</returns>
        public bool Transaction()
        {
            return _transaction;
        }

        /// <summary>
        /// Set the transaction state for this instance.
        /// </summary>
        /// <param name="set">Value to set - true to enable transactions, false to disabled.</param>
        /// <returns>Editor instance for chaining</returns>
        public Editor Transaction(bool set)
        {
            _transaction = set;
            return this;
        }

        /// <summary>
        /// Enable (default) / disable the error catching that Editor performs when
        /// processing the data from the client. When enabled any errors will be presented
        /// in a format that can be presented to the end user, but it makes debugging
        /// much more difficult if an error should occur inside the DataTables dll.
        /// Disabling the try / catch makes it much easier to see exactly where the error
        /// is occuring.
        /// </summary>
        /// <param name="set">Enable - true, or disable - false</param>
        /// <returns>Editor instance for chaining</returns>
        public Editor TryCatch(bool set)
        {
            _tryCatch = set;
            return this;
        }

        /// <summary>
        /// Get the primary key field that has been configured.
        /// 
        /// The primary key must be known to Editor so it will know which rows are being
        /// edited / deleted upon those actions. The default value is 'id'.
        /// </summary>
        /// <returns>Primary key</returns>
        public string[] Pkey()
        {
            return _pkey;
        }

        /// <summary>
        /// Set the primary key field to use. Please note that at this time
        /// Editor does not support composite primary keys in a table, only a
        /// single field primary key is supported.
        /// 
        /// The primary key must be known to Editor so it will know which rows are being
        /// edited / deleted upon those actions. The default value is 'id'.
        /// </summary>
        /// <param name="id">Primary key column name</param>
        /// <returns>Self for chaining</returns>
        public Editor Pkey(string id)
        {
            _pkey = new[] { id };
            return this;
        }

        /// <summary>
        /// Set the column names for a compound primary key.
        /// </summary>
        /// <param name="id">Primary key column names</param>
        /// <returns>Self for chaining</returns>
        public Editor Pkey(string[] id)
        {
            _pkey = id;
            return this;
        }

        /// <summary>
        /// Convert a primary key array of field values to a combined value.
        /// </summary>
        /// <param name="row">The row of data that the primary key value should be extracted from</param>
        /// <param name="flat">Flag to indicate if the given array is flat
        /// (useful for `where` conditions) or nested for join tables.</param>
        /// <returns>The created primary key value</returns>
        public string PkeyToValue(Dictionary<string, object> row, bool flat = false)
        {
            var id = new List<string>();

            for (int i = 0, ien = _pkey.Length; i<ien; i++)
            {
                var column = _pkey[i];
                string val = null;

                if (flat)
                {
                    if (row.ContainsKey(column))
                    {
                        // Frustratingly if we just use `ToString` on a DateTime we get a local
                        // specific string, which we can't then query the database with. So we
                        // need to get an ISO string that can be used to query date time fields.
                        if (row[column] is DateTime)
                        {
                            val = ((DateTime)row[column]).ToString("s");
                        }
                        else
                        {
                            val = row[column].ToString();
                        }
                    }
                }
                else
                {
                    val = NestedData.ReadProp(column, row).ToString();
                }

                if (val == null)
                {
                    throw new Exception("Primary key element is not available in data set.");
                }

                id.Add( val );
            }

            return string.Join(_pkeySeparator(), id);
        }

        /// <summary>
        /// Convert a primary key combined value to an array of field values.
        /// </summary>
        /// <param name="value">The id that should be split apart</param>
        /// <param name="flat">Flag to indicate if the returned array should be
        /// flat(useful for `where` conditions) or nested for join tables.</param>
        /// <param name="pkey">Primary key to use. Instance default will be used
        /// if not given</param>
        /// <returns>Field values that the id was made up of.</returns>
        public Dictionary<string, object> PkeyToArray (string value, bool flat=false, string[] pkey=null)
        {
            var arr = new Dictionary<string, object>();

            if (pkey == null)
            {
                pkey = _pkey;
            }

            value = value.Replace(IdPrefix(), "");
            var idParts = value.Split(new[] {_pkeySeparator()}, StringSplitOptions.None);

            if (pkey.Length != idParts.Length)
            {
                throw new Exception("Primary key data doesn't match submitted data");
            }

            for (int i = 0, ien = idParts.Length; i < ien; i++)
            {
                if (flat)
                {
                    arr.Add(pkey[i], idParts[i]);
                }
                else
                {
                    NestedData.WriteProp(arr, pkey[i], idParts[i], typeof(string));
                }
            }

            return arr;
        }


        /// <summary>
        /// Process a request from the Editor client-side to get / set data.
        /// </summary>
        /// <param name="data">Data sent from the client-side</param>
        /// <returns>Self for chaining</returns>
        public Editor Process(DtRequest data)
        {
            if (_debug)
            {
                _db.Debug((o) =>
                {
                    _DebugInfo.Add(o);
                });
            }

            if (_tryCatch)
            {
                try
                {
                    _Process(data);
                }
                catch (Exception e)
                {
                    _out.error = e.Message;

                    if (_transaction)
                    {
                        _db.Rollback();
                    }
                }
            }
            else
            {
                _Process(data);
            }

            if (_debug)
            {
                _out.debug = _DebugInfo;
                _db.Debug(false);
            }

            return this;
        }

        /// <summary>
        /// Process a request from the Editor client-side to get / set data.
        /// For use with WebAPI's 'FormDataCollection' collection
        /// </summary>
        /// <param name="data">Data sent from the client-side</param>
        /// <returns>Self for chaining</returns>
        public Editor Process(IEnumerable<KeyValuePair<string, string>> data = null)
        {
            return Process(new DtRequest(data));
        }

        /// <summary>
        /// Process a request from the Editor client-side to get / set data.
        /// For use with MVC's 'Request.Form' collection
        /// </summary>
        /// <param name="data">Data sent from the client-side</param>
        /// <returns>Self for chaining</returns>
        public Editor Process(NameValueCollection data = null)
        {
            var list = new List<KeyValuePair<string, string>>();

            if (data != null)
            {
                foreach (var key in data.AllKeys)
                {
                    list.Add(new KeyValuePair<string, string>(key, data[key]));
                }
            }

            return Process(new DtRequest(list));
        }

        /// <summary>
        /// Process a request from the Editor client-side to get / set data.
        /// For use with an HttpRequest object
        /// </summary>
        /// <param name="request">Data sent from the client-side</param>
        /// <returns>Self for chaining</returns>
        public Editor Process(HttpRequest request)
        {
            _request = request;
            _requestFiles = request.Files;

            return Process(request.Form);
        }

        /// <summary>
        /// Process a request from the Editor client-side to get / set data.
        /// For use with an HttpRequest object
        /// </summary>
        /// <param name="request">Data sent from the client-side</param>
        /// <returns>Self for chaining</returns>
        public Editor Process(UnvalidatedRequestValues request)
        {
            _requestFiles = request.Files;

            return Process(request.Form);
        }

        /// <summary>
        /// Perform validation on a data set.
        ///
        /// Note that validation is performed on data only when the action is
        /// 'create' or 'edit'. Additionally, validation is performed on the _wire
        /// data_ - i.e. that which is submitted from the client, without formatting.
        /// Any formatting required by <code>setFormatter</code> is performed after
        /// the data from the client has been validated.
        /// </summary>
        /// <param name="response">DataTables response object</param>
        /// <param name="request">DataTables request object</param>
        /// <returns>`true` if the data is valid, `false` if not.</returns>
        public bool Validate(DtResponse response, DtRequest request)
        {
            // Validation is only performed on create and edit
            if (request.RequestType != DtRequest.RequestTypes.EditorCreate &&
                request.RequestType != DtRequest.RequestTypes.EditorEdit)
            {
                return true;
            }

            foreach (var pair in request.Data)
            {
                var values = pair.Value as Dictionary<string, object>;

                foreach (var field in _field)
                {
                    var validation = field.Validate(values, this, pair.Key.Replace(_idPrefix, ""));

                    if (validation != null)
                    {
                        response.fieldErrors.Add(new DtResponse.FieldError
                        {
                            name = field.Name(),
                            status = validation
                        });
                    }
                }

                // MJoin validation
                foreach (var mjoin in _mJoin)
                {
                    mjoin.Validate(response, this, values);
                }
            }

            return !response.fieldErrors.Any();
        }

        /// <summary>
        /// Get the global validator
        /// </summary>
        /// <returns>Validation function set - nullable</returns>
        public Func<Editor, DtRequest.RequestTypes, DtRequest, string> Validator()
        {
            return _validator;
        }

        /// <summary>
        /// Set a global validator
        /// </summary>
        /// <param name="validator">Validation function to set</param>
        /// <returns></returns>
        public Editor Validator(Func<Editor, DtRequest.RequestTypes, DtRequest, string> validator)
        {
            _validator = validator;
            return this;
        }

        /// <summary>
        /// Where condition to add to the query used to get data from the database.
        /// Multiple conditions can be added if required.
        /// 
        /// Can be used in two different ways:
        /// 
        /// * Simple case: `where( field, value, operator )`
        /// * Complex: `where( fn )`
        ///
        /// The simple case is fairly self explanatory, a condition is applied to the
        /// data that looks like `field operator value` (e.g. `name = 'Allan'`). The
        /// complex case allows full control over the query conditions by providing a
        /// closure function that has access to the database Query that Editor is
        /// using, so you can use the `where()`, `or_where()`, `and_where()` and
        /// `where_group()` methods as you require.
        ///
        /// Please be very careful when using this method! If an edit made by a user
        /// using Editor removes the row from the where condition, the result is
        /// undefined (since Editor expects the row to still be available, but the
        /// condition removes it from the result set).
        /// </summary>
        /// <param name="fn">Delegate to execute adding where conditions to the table</param>
        /// <returns>Self for chaining</returns>
        public Editor Where(Action<Query> fn)
        {
            _where.Add(new WhereCondition
            {
                Custom = fn
            });

            return this;
        }

        /// <summary>
        /// Where condition to add to the query used to get data from the database.
        /// Multiple conditions can be added if required.
        /// 
        /// Can be used in two different ways:
        /// 
        /// * Simple case: `where( field, value, operator )`
        /// * Complex: `where( fn )`
        ///
        /// The simple case is fairly self explanatory, a condition is applied to the
        /// data that looks like `field operator value` (e.g. `name = 'Allan'`). The
        /// complex case allows full control over the query conditions by providing a
        /// closure function that has access to the database Query that Editor is
        /// using, so you can use the `where()`, `or_where()`, `and_where()` and
        /// `where_group()` methods as you require.
        ///
        /// Please be very careful when using this method! If an edit made by a user
        /// using Editor removes the row from the where condition, the result is
        /// undefined (since Editor expects the row to still be available, but the
        /// condition removes it from the result set).
        /// </summary>
        /// <param name="key">Database column name to perform the condition on</param>
        /// <param name="value">Value to use for the condition</param>
        /// <param name="op">Conditional operator</param>
        /// <returns>Self for chaining</returns>
        public Editor Where(string key, object value, string op = "=")
        {
            _where.Add(new WhereCondition
            {
                Key = key,
                Value = value,
                Operator = op
            });

            return this;
        }


        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Internal methods
         */

        /// <summary>
        /// Get the request object used for this instance
        /// </summary>
        /// <returns>HTTP request object</returns>
        internal HttpFileCollection RequestFiles()
        {
            return _requestFiles;
        }

        /// <summary>
        /// Get the request object used for this instance
        /// </summary>
        /// <returns>HTTP request object</returns>
        internal HttpRequest Request()
        {
            return _request;
        }


        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Private methods
         */

        private void _Process(DtRequest data)
        {
            _out = new DtResponse();
            _processData = data;
            _formData = data.Data;

            if (_transaction)
            {
                _db.Transaction();
            }

            _PrepJoin();
            _PrepModel();

            // Global validation
            if (_validator != null)
            {
                var ret = _validator(this, data.RequestType, data);

                if (ret != "")
                {
                    _out.error = ret;
                }
            }

            if (string.IsNullOrEmpty(_out.error))
            {
                if (data.RequestType == DtRequest.RequestTypes.DataTablesGet ||
                    data.RequestType == DtRequest.RequestTypes.DataTablesSsp)
                {
                    // DataTable get request
                    _out.Merge(_Get(null, data));
                }
                else if (data.RequestType == DtRequest.RequestTypes.EditorUpload)
                {
                    // File upload
                    _Upload(data);
                }
                else if (data.RequestType == DtRequest.RequestTypes.EditorRemove)
                {
                    // Remove rows
                    _Remove(data);
                    _FileClean();
                }
                else
                {
                    // Create or edit
                    // Trigger pre events before validation, so validation could be added
                    var cancelResult = false;
                    var toRemove = new List<string>();
                    
                    foreach (var pair in data.Data)
                    {
                        if (data.RequestType == DtRequest.RequestTypes.EditorCreate)
                        {
                            if (PreCreate != null)
                            {
                                var args = new PreCreateEventArgs
                                {
                                    Editor = this,
                                    Values = pair.Value as Dictionary<string, object>
                                };
                                PreCreate(this, args);

                                cancelResult = args.Cancel;
                            }
                        }
                        else
                        {
                            if (PreEdit != null)
                            {
                                var args = new PreEditEventArgs
                                {
                                    Editor = this,
                                    Id = pair.Key.Replace(_idPrefix, ""),
                                    Values = pair.Value as Dictionary<string, object>
                                };
                                PreEdit(this, args);

                                cancelResult = args.Cancel;
                            }
                        }

                        // If the event handlers cancelled the update
                        if (cancelResult)
                        {
                            // Remove the data from the data set so it won't be processed
                            toRemove.Add(pair.Key);

                            // Tell the client-side we aren't updating this row
                            _out.cancelled.Add(pair.Key);
                        }
                    }

                    // Tidy up anything that should be removed
                    foreach (var removeId in toRemove)
                    {
                        data.Data.Remove(removeId);
                    }

                    // Validate
                    var valid = Validate(_out, data);
                        
                    if (valid)
                    {
                        foreach (var pair in data.Data)
                        {
                            var d = data.RequestType == DtRequest.RequestTypes.EditorCreate
                                ? _Insert(pair.Value as Dictionary<string, object>)
                                : _Update(pair.Key, pair.Value as Dictionary<string, object>);

                            if (d != null)
                            {
                                _out.data.Add(d);
                            }
                        }
                    }

                    _FileClean();
                }
            }

            if (_transaction)
            {
                _db.Commit();
            }
        }

        private void _PrepModel()
        {
            // Add fields which are defined in the model, but not in the _Field list
            if (_userModelT.Count != 0)
            {
                foreach (KeyValuePair<string, Type> pair in _userModelT)
                {
                    _FieldFromModel(pair.Value, pair.Key=="" ? pair.Key : pair.Key+".");
                }
            }
        }

        private void _FieldFromModel(Type model, string parent = "")
        {
            // Add the properties
            foreach (var pi in model.GetProperties())
            {
                var field = _FindField(parent + pi.Name, "name");

                // If the field doesn't exist yet, create it
                if (field == null)
                {
                    field = new Field(parent + pi.Name);
                    Field(field);
                }

                // Then assign the information from the model
                field.Type(pi.PropertyType);

                var prop = pi.GetCustomAttributes(typeof(EditorTypeErrorAttribute), true);
                var err = prop
                     .Cast<EditorTypeErrorAttribute>().FirstOrDefault();

                if (err != null)
                {
                    field.TypeError(err.Msg);
                }

                var name = pi.GetCustomAttributes(typeof(EditorHttpNameAttribute), false)
                     .Cast<EditorHttpNameAttribute>().FirstOrDefault();

                if (name != null)
                {
                    field.Name(name.Name);
                }
            }

            // Add any nested classes and their properties
            var nested = model.GetNestedTypes(BindingFlags.Public | BindingFlags.Instance);

            foreach (var t in nested)
            {
                _FieldFromModel(t, parent + t.Name + ".");
            }
        }



        private DtResponse _Get(object id = null, DtRequest http = null)
        {
            var dtData = new DtResponse();

            if (PreGet != null)
            {
                var args = new PreGetEventArgs
                {
                    Editor = this,
                    Id = id
                };
                PreGet(this, args);

                if (args.Cancel)
                {
                    return dtData;
                }
            }

            Query query = _db
                .Query("select")
                .Table(_table)
                .Get(_pkey);

            // Add all fields that we need to get from the database
            foreach (var field in _field)
            {
                // Don't reselect a pkey column if it was already added
                if (_pkey.Contains(field.DbField()))
                {
                    continue;
                }

                if (field.Apply("get") && field.GetValue() == null)
                {
                    query.Get(field.DbField());
                }
            }

            _GetWhere(query);
            _PerformLeftJoin(query);
            var ssp = _SspQuery(query, http);

            if (id != null)
            {
                query.Where(PkeyToArray(id.ToString(), true));
            }

            var res = query.Exec();
            Dictionary<string, object> row;

            while ((row = res.Fetch()) != null)
            {
                var inner = new Dictionary<string, object> { { "DT_RowId", _idPrefix + PkeyToValue(row, true) } };

                foreach (var field in _field)
                {
                    if (field.Apply("get"))
                    {
                        field.Write(inner, row);
                    }
                }

                dtData.data.Add(inner);
            }

            // Field options
            if (id == null)
            {
                foreach (var field in _field)
                {
                    var opts = field.OptionsExec(_db);

                    if (opts != null)
                    {
                        dtData.options.Add(field.Name(), opts);
                    }
                }
            }

            // Row based joins
            foreach (var mjoin in _mJoin)
            {
                mjoin.Data(this, dtData);
            }

            // Uploaded files
            dtData.files = _FileData();

            if (PostGet != null)
            {
                var args = new PostGetEventArgs
                {
                    Editor = this,
                    Id = id,
                    Data = dtData.data
                };
                PostGet(this, args);
            }

            return dtData.Merge(ssp);
        }


        private Dictionary<string, object> _Insert(Dictionary<string, object> values)
        {
            // Only allow a composite insert if the values for the key are
            // submitted. This is required because there is no reliable way in MySQL
            // to return the newly inserted row, so we can't know any newly
            // generated values.
            _PkeyValidateInsert(values);

            // Insert the new row
            var id = _InsertOrUpdate(null, values);

            if (id == null)
            {
                return null;
            }

            // Was the primary key altered as part of the edit, if so use the
            // submitted values
            id = _pkey.Length > 1
                ? PkeyToValue(values)
                : _PkeySubmitMerge(id.ToString(), values);
            
            // Row based joins
            foreach (var mjoin in _mJoin)
            {
                mjoin.Insert(this, id, values);
            }

            // _Trigger("WirteCreate", id, values);
            WriteCreate?.Invoke(this, new WriteCreateEventArgs
            {
                Editor = this,
                Values = values,
                Id = id
            });

            // Full data set for the created row
            var row = _Get(id);
            var rowData = row.data.Any() ?
                row.data[0] :
                null;

            // _Trigger("postCreate", id, values, row);
            PostCreate?.Invoke(this, new PostCreateEventArgs
            {
                Editor = this,
                Values = values,
                Data = rowData,
                Id = id
            });

            return rowData;
        }


        private Dictionary<string, object> _Update(string id, Dictionary<string, object> values)
        {
            id = id.Replace(_idPrefix, "");

            // Update or insert the rows for the parent table and the left joined
            // tables
            _InsertOrUpdate(id, values);

            // Row based joins
            foreach (var mjoin in _mJoin)
            {
                mjoin.Update(this, id, values);
            }

            // Was the primary key altered as part of the edit, if so use the
            // submitted values
            var getId = _PkeySubmitMerge(id, values);

            WriteEdit?.Invoke(this, new WriteEditEventArgs
            {
                Editor = this,
                Id = id,
                Values = values
            });

            // Full data set for the modified row
            var row = _Get(getId);
            var rowData = row.data.Any() ?
                row.data[0] :
                null;

            PostEdit?.Invoke(this, new PostEditEventArgs
            {
                Editor = this,
                Id = id,
                Data = rowData,
                Values = values
            });

            return rowData;
        }


        private void _Remove(DtRequest data)
        {
            // Strip the ID prefix that the client-side sends back
            var ids = new List<string>();

            foreach (var pair in data.Data)
            {
                var id = pair.Key.Replace(_idPrefix, "");

                if (PreRemove != null)
                {
                    var args = new PreRemoveEventArgs
                    {
                        Editor = this,
                        Id = id,
                        Values = pair.Value as Dictionary<string, object>
                    };
                    PreRemove(this, args);

                    if (args.Cancel)
                    {
                        _out.cancelled.Add(pair.Key);
                    }
                    else
                    {
                        ids.Add(id);
                    }
                }
                else
                {
                    ids.Add(id);
                }
            }

            if (!ids.Any())
            {
                return;
            }

            // Row based joins - remove first as the host row will be removed which is
            // a dependency
            foreach (var mjoin in _mJoin)
            {
                mjoin.Remove(this, ids);
            }

            // Remove from the left join tables
            for (int i = 0, ien = _leftJoin.Count(); i < ien; i++)
            {
                var join = _leftJoin[i];
                string parentLink;
                string childLink;


                // which side of the join refers to the parent table?
                if (join.Field1.IndexOf(join.Table) == 0)
                {
                    parentLink = join.Field2;
                    childLink = join.Field1;
                }
                else
                {
                    parentLink = join.Field1;
                    childLink = join.Field2;
                }

                // Only delete on the primary key, since that is what the ids refer
                // to - otherwise we'd be deleting random data! Note that this
                // won't work with compound keys since the parent link would be
                // over multiple fields.
                if (_pkey.Length == 1 && parentLink == _pkey[0])
                {
                    _RemoveTable(join.Table, ids, new[] {childLink});
                }
            }

            // Remove from the primary tables
            for (int i = 0, ien = _table.Count(); i < ien; i++)
            {
                _RemoveTable(_table[i], ids);
            }

            foreach (var pair in data.Data)
            {
                var id = pair.Key.Replace(_idPrefix, "");

                PostRemove?.Invoke(this, new PostRemoveEventArgs
                {
                    Editor = this,
                    Id = id,
                    Values = pair.Value as Dictionary<string, object>
                });
            }
        }


        private void _Upload(DtRequest data)
        {
            if (_requestFiles == null)
            {
                throw new Exception("File upload requires that 'Process' be called with an HttpRequest or UnvalidatedRequestValues object");
            }

            var field = _FindField(data.UploadField, "name");
            var fieldName = "";

            if (field == null)
            {
                // Perhaps it in a join instance
                for (var i = 0; i < _mJoin.Count(); i++)
                {
                    var join = _mJoin[i];

                    foreach (var joinField in join.Fields())
                    {
                        var name = join.Name() + "[]." + joinField.Name();

                        if (name == data.UploadField)
                        {
                            field = joinField;
                            fieldName = name;
                        }
                    }

                }
            }
            else
            {
                fieldName = field.Name();
            }

            if (field == null)
            {
                throw new Exception("Unknown upload field name submitted");
            }

            if (PreUpload != null)
            {
                var args = new PreUploadEventArgs
                {
                    Editor = this,
                    Data = data
                };
                PreUpload(this, args);

                if (args.Cancel)
                {
                    return;
                }
            }

            var upload = field.Upload();
            if (upload == null)
            {
                throw new Exception("File uploaded to a field that does not have upload options configured");
            }

            object res = upload.Exec(this);

            if (res is Boolean && (Boolean)res == false)
            {
                _out.fieldErrors.Add(new DtResponse.FieldError
                {
                    name = fieldName,
                    status = upload.Error()
                });
            }
            else
            {
                _out.files = _FileData(upload.Table(), res);
                _out.upload.id = res;

                PostUpload?.Invoke(this, new PostUploadEventArgs
                {
                    Editor = this,
                    Id = res,
                    Files = _out.files,
                    Data = data
                });
            }
        }


        private Dictionary<string, Dictionary<string, Dictionary<string, object>>> _FileData(string limitTable = null, object id = null)
        {
            var files = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();

            // The fields in this instance
            _FileDataFields(files, _field, limitTable, id);

            // From joined tables
            foreach (var join in _mJoin)
            {
                _FileDataFields(files, join.Fields(), limitTable, id);
            }

            return files;
        }


        private void _FileDataFields(IDictionary<string, Dictionary<string, Dictionary<string, object>>> @files,
            IEnumerable<Field> fields, string limitTable, object id)
        {
            foreach (var field in fields)
            {
                var upload = field.Upload();

                if (upload == null)
                {
                    continue;
                }

                var table = upload.Table();

                if (table == null)
                {
                    continue;
                }

                if (limitTable != null && table != limitTable)
                {
                    continue;
                }

                if (files.ContainsKey(table))
                {
                    continue;
                }

                var fileData = upload.Data(_db, id);

                if (fileData != null)
                {
                    files.Add(table, fileData);
                }
            }
        }


        private void _FileClean()
        {
            foreach (var field in _field)
            {
                var upload = field.Upload();

                if (upload != null)
                {
                    upload.DbCleanExec(this, field);
                }
            }

            foreach (var join in _mJoin)
            {
                foreach (var field in join.Fields())
                {
                    var upload = field.Upload();

                    if (upload != null)
                    {
                        upload.DbCleanExec(this, field);
                    }
                }
            }
        }


        private void _RemoveTable(string table, IEnumerable<string> ids, string[] pkey = null)
        {
            if (pkey == null)
            {
                pkey = _pkey;
            }

            // Check that there is a field which has a set option for this table
            var count = _field.Count(
                field => !field.DbField().Contains(".") || (
                    _Part(field.DbField()) == table &&
                    field.Set() != DataTables.Field.SetType.None
                )
            );

            if (count > 0)
            {
                var q = _db.Query("delete")
                    .Table(table);

                foreach (var id in ids)
                {
                    var cond = PkeyToArray(id, true, pkey);

                    q.OrWhere(q2 =>
                    {
                        q2.Where(cond);
                    });
                }

                q.Exec();
            }
        }


        private void _PrepJoin()
        {
            if (!_leftJoin.Any())
            {
                return;
            }

            // Check if the primary key has a table identifier - if not, add one
            for (int i = 0, ien = _pkey.Length; i < ien; i++)
            {
                if (!_pkey[i].Contains('.'))
                {
                    _pkey[i] = _Alias(_table[0]) + '.' + _pkey[i];
                }
            }

            // Check that all the fields have a table selector, orhterwise, we'd need to
            // know the structure of the tables, to know which fields belong in
            // which. This extra requirement on the fields removed that
            foreach (var field in _field)
            {
                var name = field.DbField();

                if (!name.Contains('.'))
                {
                    throw new Exception("Table part of the field '" + name + "' was not found. " +
                        "In Editor instance that use a join, all the fields must have the " +
                        "database table set explicity."
                    );
                }
            }
        }


        private Field _FindField(string name, string type)
        {
            for (int i = 0, ien = _field.Count(); i < ien; i++)
            {
                var field = _field[i];

                if (type == "name" && field.Name() == name)
                {
                    return field;
                }

                if (type == "db" && field.DbField() == name)
                {
                    return field;
                }
            }

            return null;
        }


        private void _GetWhere(Query query)
        {
            foreach (var where in _where)
            {
                if (where.Custom != null)
                {
                    where.Custom(query);
                }
                else
                {
                    query.Where(where.Key, where.Value, where.Operator);
                }
            }
        }


        private DtResponse _SspQuery(Query query, DtRequest http)
        {
            var ssp = new DtResponse();

            if (http == null || http.RequestType == DtRequest.RequestTypes.DataTablesGet)
            {
                return ssp;
            }

            // Add the server-side processing conditions
            _SspLimit(query, http);
            _SspSort(query, http);
            _SspFilter(query, http);

            // Get the nuber of rows in the result set
            var setCount = _db
                .Query("select")
                .Table(_table)
                .Get("COUNT( " + _pkey[0] + " ) as cnt");
            _GetWhere(setCount);
            _SspFilter(setCount, http);
            _PerformLeftJoin(setCount);

            var setCounted = Convert.ToInt32(setCount.Exec().Fetch()["cnt"]);

            // Get the number of rows in the full set
            var fullCount = _db
                .Query("select")
                .Table(_table)
                .Get("COUNT( " + _pkey[0] + " ) as cnt");
            _GetWhere(fullCount);

            // A left join is only needed if there is a where condition, incase the
            // conditional items are the ones being joined in
            if (_where.Any())
            {
                _PerformLeftJoin(fullCount);
            }

            var fullCounted = Convert.ToInt32(fullCount.Exec().Fetch()["cnt"]);

            ssp.draw = http.Draw;
            ssp.recordsTotal = fullCounted;
            ssp.recordsFiltered = setCounted;

            return ssp;
        }


        private string _SspField(DtRequest http, int index)
        {
            var name = http.Columns[index].Data;
            var field = _FindField(name, "name");

            if (field != null)
            {
                return field.DbField();
            }

            // Is it the primary key?
            if (name == "DT_RowId")
            {
                return _pkey[0];
            }

            throw new Exception("Unknown field: " + name + " (index " + index + ")");
        }


        private void _SspSort(Query query, DtRequest http)
        {
            for (int i = 0, ien = http.Order.Count(); i < ien; i++)
            {
                var order = http.Order[i];

                query.Order(
                    _SspField(http, order.Column) + " " +
                    (order.Dir == "asc" ? "asc" : "desc")
                );
            }
        }


        private void _SspFilter(Query query, DtRequest http)
        {
            // Global search, add a ( ... or ... ) set of filters for each column
            // in the table (not the fields, just the columns submitted)
            if (http.Search.Value != "")
            {
                query.Where(delegate(Query q)
                {
                    for (int i = 0, ien = http.Columns.Count(); i < ien; i++)
                    {
                        if (!http.Columns[i].Searchable)
                        {
                            continue;
                        }

                        var field = _SspField(http, i);

                        if (field != null)
                        {
                            q.OrWhere(field, "%" + http.Search.Value + "%", "like");
                        }
                    }
                });
            }

            // Column filters
            for (int i = 0, ien = http.Columns.Count(); i < ien; i++)
            {
                var column = http.Columns[i];
                var search = column.Search.Value;

                if (search != "" && column.Searchable)
                {
                    query.Where(_SspField(http, i), "%" + search + "%", "like");
                }
            }
        }


        private void _SspLimit(Query query, DtRequest http)
        {
            // -1 is "show all" in DataTables, so there would be no limit at that point
            if (http.Length != -1)
            {
                query
                    .Offset(http.Start)
                    .Limit(http.Length);
            }
        }


        private object _InsertOrUpdate(object id, Dictionary<string, object> values)
        {
            // Loop over all tables in _Table, doing the insert of update as needed
            for (int i = 0, ien = _table.Count(); i < ien; i++)
            {
                var res = _InsertOrUpdateTable(
                    _table[i],
                    values,
                    id == null ?
                        null :
                        PkeyToArray(id.ToString(), true)
                );

                // If we don't have an id yet, then the first insert will return
                // the id we want.
                if (res != null && id == null)
                {
                    id = res.InsertId();
                }
            }

            // And for the left join tables as well
            foreach (var join in _leftJoin)
            {
                string parentLink;
                string childLink;
                var where = new Dictionary<string, object>();
                object whereValue;

                // Which side of the join refers to the parent table?
                var joinTable = _Alias(join.Table);
                var tablePart = _Part(join.Field1);

                if (_Part(join.Field1, "db") != null)
                {
                    tablePart = _Part(join.Field1, "db") + "." + tablePart;
                }

                if (tablePart == joinTable)
                {
                    parentLink = join.Field2;
                    childLink = join.Field1;
                }
                else
                {
                    parentLink = join.Field1;
                    childLink = join.Field2;
                }

                if (_pkey.Length == 1 && parentLink == _pkey[0])
                {
                    whereValue = id;
                }
                else
                {
                    // We need submitted information about the joined data to be
                    // submitted as well as the new value. We first check if the
                    // host field was submitted
                    var field = _FindField(parentLink, "db");

                    if (field == null || !field.Apply("set", values))
                    {
                        // If not, then check if the child id was submitted
                        field = _FindField(childLink, "db");

                        // No data available, so we can't do anything
                        if (field == null || !field.Apply("set", values))
                        {
                            continue;
                        }
                    }

                    whereValue = field.Val("set", values);
                }

                where.Add(_Part(childLink, "field"), whereValue);
                _InsertOrUpdateTable(join.Table, values, where);
            }

            return id;
        }


        private Result _InsertOrUpdateTable(string table, Dictionary<string, object> values, Dictionary<string, object> where)
        {
            var set = new Dictionary<string, object>();
            var action = where == null ? "create" : "edit";
            var tableAlias = _Alias(table);

            foreach (var field in _field)
            {
                var tablePart = _Part(field.DbField());

                if (_Part(field.DbField(), "db") != null)
                {
                    tablePart = _Part(field.DbField(), "db") + "." + tablePart;
                }

                // Does this field apply to this table (only check when a join is
                // being used)
                if (_leftJoin.Any() && tablePart != tableAlias)
                {
                    continue;
                }

                // /Check if this field should be set, based on the options and
                // submitted data
                if (!field.Apply(action, values))
                {
                    continue;
                }

                // Some db's (specifically postgres) don't like having the table
                // name prefixing the column name.
                var fieldPart = _Part(field.DbField(), "field");
                set.Add(fieldPart, field.Val("set", values));
            }

            // If nothing to do, then do nothing!
            if (!set.Any())
            {
                return null;
            }

            // Insert or update
            return action == "create" ?
                _db.Insert(table, set, _pkey) :
                _db.Push(table, set, @where, _pkey);
        }


        private void _PerformLeftJoin(Query q)
        {
            if (_leftJoin.Count() != 0)
            {
                foreach (var join in _leftJoin)
                {
                    q.Join(join.Table, join.Field1 + " " + join.Operator + " " + join.Field2, "LEFT");
                }
            }
        }


        private string _Alias(string name, string type = "alias")
        {
            if (name.IndexOf(" as ", StringComparison.OrdinalIgnoreCase) > 0)
            {
                var a = Regex.Split(name, " as ", RegexOptions.IgnoreCase);
                return type == "alias"
                    ? a[1]
                    : a[0];
            }

            if (name.IndexOf(" ", StringComparison.OrdinalIgnoreCase) > 0)
            {
                var a = Regex.Split(name, " ", RegexOptions.IgnoreCase);
                return type == "alias"
                    ? a[1]
                    : a[0];
            }

            return name;

        }


        private string _Part(string name, string type = "table")
        {
            string db = null;
            string table = null;
            string column = null;

            if (name.Contains("."))
            {
                var a = name.Split('.');

                if (a.Count() == 3)
                {
                    db = a[0];
                    table = a[1];
                    column = a[2];
                }
                else if (a.Count() == 2)
                {
                    table = a[0];
                    column = a[1];
                }
            }
            else
            {
                column = name;
            }

            switch (type)
            {
                case "db":
                    return db;
                case "table":
                    return table;
                default:
                    return column;
            }
        }

        private string _PkeySubmitMerge(string pkeyVal, Dictionary<string, object> row)
        {
            var arr = PkeyToArray(pkeyVal, true);

            for (int i = 0, ien = _pkey.Length; i < ien; i++)
            {
                var column = _pkey[i];
                var field = _FindField(column, "db");

                if (field != null && field.Apply("edit", row))
                {
                    if (arr.ContainsKey(column))
                    {
                        arr[column] = field.Val("set", row);
                    }
                    else
                    {
                        arr.Add(column, field.Val("set", row));
                    }
                }
            }

            return PkeyToValue(arr, true);
        }


        private bool _PkeyValidateInsert(Dictionary<string, object> row)
        {
            if (_pkey.Length == 1)
            {
                return true;
            }

            for (int i = 0, ien = _pkey.Length; i < ien; i++)
            {
                var column = _pkey[i];
                var field = _FindField(column, "db");

                if (field == null || !field.Apply("create", row))
                {
                    throw new Exception("When inserting into a compound key table, " +
                                        "all fields that are part of the compound key must be " +
                                        "submitted with a specific value.");
                }
            }
            return true;
        }


        private string _pkeySeparator()
        {
            var pkeys = string.Join(",", _pkey);

            return pkeys.GetHashCode().ToString("X");
        }
    }
}
