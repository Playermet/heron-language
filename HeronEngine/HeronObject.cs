﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    public class HObject
    {
        string type = "undefined";

        public static HObject Void = new HObject("void");
        public static HObject Null = new HObject("null");

        public HObject()
        {            
        }

        public HObject(string type)
        {
            this.type = type;
        }

        public string GetHeronType()
        {
            return type;
        }

        public virtual Object ToDotNetObject()
        {
            throw new Exception("Cannot convert " + type + " into System.Object");
        }

        public virtual bool ToBool()
        {
            throw new Exception("Cannot convert " + type + " into System.Boolean");
        }

        public virtual HObject GetAt(HObject index)
        {
            throw new Exception("unimplemented");
        }

        public virtual void SetAt(HObject index, HObject val)
        {
            throw new Exception("unimplemented");
        }
    }

    public class SystemObject : HObject
    {
        Object obj;

        public SystemObject(Object obj)
        {
            this.obj = obj;
        }

        public override Object ToDotNetObject()
        {
            return obj;
        }

        public override bool ToBool()
        {
            // TODO: add more checks
            return (bool)obj;
        }

        public override string ToString()
        {
            return obj.ToString();
        }
    }

    public class Collection : HObject
    {
    }

    /// <summary>
    /// An instance of a Heron class.
    /// </summary>
    public class Instance : HObject
    {
        public Class hclass;
        public ObjectTable fields = new ObjectTable();

        public Instance(Class c)
        {
            hclass = c;
        }

        /// <summary>
        /// Creates a scope in the environment, containing variables that map to the class field names. 
        /// It is the caller's reponsibility to remove the scope. 
        /// </summary>
        /// <param name="env"></param>
        public void PushFieldsAsScope(Environment env)
        {
            env.PushScope(fields);
        }

        /// <summary>
        /// Mostly for internal purposes. 
        /// </summary>
        /// <param name="name"></param>
        public void AssureFieldDoesntExist(string name)
        {
            if (HasField(name))
                throw new Exception("field " + name + " already exists");
        }

        /// <summary>
        /// Mostly for internal purposes
        /// </summary>
        /// <param name="name"></param>
        public void AssureFieldExists(string name)
        {
            if (!HasField(name))
                throw new Exception("field " + name + " does not exist");
        }

        /// <summary>
        /// Sets the value on the named field. Does not automatically add a field if missing.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void SetFieldValue(string name, HObject val)
        {
            AssureFieldExists(name);
            fields[name] = val;
        }

        /// <summary>
        /// Returns true if field has already been added 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasField(string name)
        {
            return fields.ContainsKey(name);
        }

        /// <summary>
        /// Adds a field. Field must not already exist. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddField(string name, HObject val)
        {
            AssureFieldDoesntExist(name);
            fields.Add(name, val);
        }

        /// <summary>
        /// Adds a field if it does not exist, otherwise simple sets the value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddOrSetFieldValue(string name, HObject val)
        {
            if (HasField(name))
                fields[name] = val;
            fields.Add(name, val);
        }

        /// <summary>
        /// Returns a value for the named field. The field must exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public HObject GetFieldValue(string name)
        {
            AssureFieldExists(name);
            return fields[name];
        }

        public override string ToString()
        {
            string r = hclass.name;
            r += " = { ";
            foreach (string key in fields.Keys)
            {
                string val = fields[key].ToString();
                r += key + " = " + val + "; ";
            }
            r += " }";
            return r;
        }
    }
}
