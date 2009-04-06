﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace HeronEngine
{
    public class HeronObject
    {
        string type = "undefined";

        public static HeronObject Void = new HeronObject("void");
        public static HeronObject Null = new HeronObject("null");

        public HeronObject()
        {            
        }

        public HeronObject(string type)
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

        public virtual HeronObject GetAt(HeronObject index)
        {
            throw new Exception("unimplemented");
        }

        public virtual void SetAt(HeronObject index, HeronObject val)
        {
            throw new Exception("unimplemented");
        }

        public virtual HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("method invocation not supported on " + ToString());
        }

        public virtual HeronObject InvokeUnaryOperator(string s)
        {
            throw new Exception("unary operator invocation not supported on " + ToString());
        }

        public virtual HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            throw new Exception("binary operator invocation not supported on " + ToString());
        }

    }

    public class DotNetObject : HeronObject
    {
        Object obj;

        public DotNetObject(Object obj)
        {
            this.obj = obj;
        }

        public override Object ToDotNetObject()
        {
            return obj;
        }

        public override string ToString()
        {
            return obj.ToString();
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            Object[] objs = HeronType.HeronObjectArrayToDotNetArray(args);
            Type[] types = HeronType.ObjectsToTypes(objs);
            Type type = obj.GetType();
            MethodInfo mi = type.GetMethod(s, types);
            if (mi == null)
                throw new Exception("unable to find  method " + s + " on the dot net object " + obj.ToString() + " with supplied argument types");
            Object r = mi.Invoke(self.ToDotNetObject(), objs);
            return new DotNetObject(r);
        }
    }

    public class PrimitiveObject<T> : HeronObject 
    {
        T val;

        public PrimitiveObject(T x)
        {
            val = x;
        }

        public PrimitiveObject()
        {
        }

        public override string ToString()
        {
            return val.ToString();
        }

        public override object ToDotNetObject()
        {
            return val;
        }

        public T GetValue()
        {
            return val;
        }
    }

    public class IntObject : PrimitiveObject<int>
    {
        public IntObject(int x)
            : base(x)
        {
        }

        public IntObject()
        {
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("No methods available on Int object");
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new IntObject(-GetValue());
                case "~": return new IntObject(~GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is IntObject))
                throw new Exception("binary operation not supported on differently typed objects");

            int arg = (x as IntObject).GetValue();
            switch (s)
            {
                case "+": return new IntObject(GetValue() + arg);
                case "-": return new IntObject(GetValue() - arg);
                case "*": return new IntObject(GetValue() * arg);
                case "/": return new IntObject(GetValue() / arg);
                case "%": return new IntObject(GetValue() % arg);
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "<": return new BoolObject(GetValue() < arg);
                case ">": return new BoolObject(GetValue() > arg);
                case "<=": return new BoolObject(GetValue() <= arg);
                case ">=": return new BoolObject(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by integers");
            }
        }
    }

    public class CharObject : PrimitiveObject<char>
    {
        public CharObject(char x)
            : base(x)
        {
        }

        public CharObject()
        {
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("No methods available on Char object");
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by chars");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            switch (s)
            {
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by chars");
            }
        }
    }

    public class FloatObject : PrimitiveObject<double>
    {
        public FloatObject(double x)
            : base(x)
        {
        }

        public FloatObject()
        {
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("No methods available on Float object");
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "-": return new FloatObject(-GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x) 
        {
            if (!(x is FloatObject))
                throw new Exception("binary operation not supported on differently typed objects");
            double arg = (x as FloatObject).GetValue();
            switch (s)
            {
                case "+": return new FloatObject(GetValue() + arg);
                case "-": return new FloatObject(GetValue() - arg);
                case "*": return new FloatObject(GetValue() * arg);
                case "/": return new FloatObject(GetValue() / arg);
                case "%": return new FloatObject(GetValue() % arg);
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "<": return new BoolObject(GetValue() < arg);
                case ">": return new BoolObject(GetValue() > arg);
                case "<=": return new BoolObject(GetValue() <= arg);
                case ">=": return new BoolObject(GetValue() >= arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by floats");
            }
        }
    }

    public class BoolObject : PrimitiveObject<bool>
    {
        public BoolObject(bool x)
            : base(x)
        {
        }

        public BoolObject()
        {
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("No methods available on Bool object");
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                case "!": return new BoolObject(!GetValue());
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by booleans");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is BoolObject))
                throw new Exception("binary operation not supported on differently typed objects");
            bool arg = (x as BoolObject).GetValue();
            switch (s)
            {
                case "==": return new BoolObject(GetValue() == arg);
                case "!=": return new BoolObject(GetValue() != arg);
                case "&&": return new BoolObject(GetValue() && arg);
                case "||": return new BoolObject(GetValue() || arg);
                case "^^": return new BoolObject(GetValue() ^ arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by booleans");
            }
        }
    }

    public class StringObject : PrimitiveObject<string>
    {
        public StringObject(string x)
            : base(x)
        {
        }

        public StringObject()
        {
        }

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            throw new Exception("No methods available on String object");
        }

        public override HeronObject InvokeUnaryOperator(string s)
        {
            switch (s)
            {
                default:
                    throw new Exception("Unary operation: '" + s + "' not supported by integers");
            }
        }

        public override HeronObject InvokeBinaryOperator(string s, HeronObject x)
        {
            if (!(x is StringObject))
                throw new Exception("binary operation not supported on differently typed objects");
            string arg = (x as StringObject).GetValue();
            switch (s)
            {
                case "+": return new StringObject(GetValue() + arg);
                default:
                    throw new Exception("Binary operation: '" + s + "' not supported by strings");
            }
        }
    }

    public class ListObject : HeronObject
    {
        List<HeronObject> list = new List<HeronObject>();
    }

    /// <summary>
    /// An instance of a Heron class. A HeronObject is more general in that it includes 
    /// primitive objects and .NET objects which are not part of the HeronClass 
    /// hierarchy.
    /// </summary>
    public class Instance : HeronObject
    {
        public HeronClass hclass;
        public ObjectTable fields = new ObjectTable();

        public Instance(HeronClass c)
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
        public void SetFieldValue(string name, HeronObject val)
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
        public void AddField(string name, HeronObject val)
        {
            AssureFieldDoesntExist(name);
            fields.Add(name, val);
        }

        /// <summary>
        /// Adds a field if it does not exist, otherwise simple sets the value. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void AddOrSetFieldValue(string name, HeronObject val)
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
        public HeronObject GetFieldValue(string name)
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

        public override HeronObject Invoke(Environment env, string s, HeronObject self, HeronObject[] args)
        {
            Function f = hclass.FindMethod(s, args);
            throw new Exception("unimplemented");
        }
    }
}
