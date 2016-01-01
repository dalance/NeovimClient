using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NeovimClient {

    public static class NeovimExt {
        public static long AsInt64orExt( this MessagePackObject o ) {
            if( (bool)o.IsTypeOf<long>() ) {
                return o.AsInt64();
            }

            var bytes = o.AsMessagePackExtendedTypeObject().GetBody();
            if ( bytes.Length < 8 ) {
                Array.Resize( ref bytes, 8 );
            }

            if ( !BitConverter.IsLittleEndian ) {
                Array.Reverse( bytes );
            }

            return BitConverter.ToInt64( bytes, 0 );
        }
    }

    public class NeovimClient<T> : IDisposable where T : INeovimIO {

        // - field -----------------------------------------------------------------------

        private T neovimIO;
        private Dictionary<string, object> api = new Dictionary<string, object>();

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimClient( T io ) {
            neovimIO = io;
            CreateApi();
        }

        public void Dispose() {
            neovimIO.Dispose();
        }

        public void Action                        ( string name                                           ) { ( api[name] as Action                         )(                        ); }
        public void Action<T1                    >( string name, T1 p0                                    ) { ( api[name] as Action<T1                    > )( p0                     ); }
        public void Action<T1, T2                >( string name, T1 p0, T2 p1                             ) { ( api[name] as Action<T1, T2                > )( p0, p1                 ); }
        public void Action<T1, T2, T3            >( string name, T1 p0, T2 p1, T3 p2                      ) { ( api[name] as Action<T1, T2, T3            > )( p0, p1, p2             ); }
        public void Action<T1, T2, T3, T4        >( string name, T1 p0, T2 p1, T3 p2, T4 p3               ) { ( api[name] as Action<T1, T2, T3, T4        > )( p0, p1, p2, p3         ); }
        public void Action<T1, T2, T3, T4, T5    >( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4        ) { ( api[name] as Action<T1, T2, T3, T4, T5    > )( p0, p1, p2, p3, p4     ); }
        public void Action<T1, T2, T3, T4, T5, T6>( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5 ) { ( api[name] as Action<T1, T2, T3, T4, T5, T6> )( p0, p1, p2, p3, p4, p5 ); }

        public T1 Func<T1                        >( string name                                           ) { return ( api[name] as Func<T1                        > )(                        ); }
        public T2 Func<T1, T2                    >( string name, T1 p0                                    ) { return ( api[name] as Func<T1, T2                    > )( p0                     ); }
        public T3 Func<T1, T2, T3                >( string name, T1 p0, T2 p1                             ) { return ( api[name] as Func<T1, T2, T3                > )( p0, p1                 ); }
        public T4 Func<T1, T2, T3, T4            >( string name, T1 p0, T2 p1, T3 p2                      ) { return ( api[name] as Func<T1, T2, T3, T4            > )( p0, p1, p2             ); }
        public T5 Func<T1, T2, T3, T4, T5        >( string name, T1 p0, T2 p1, T3 p2, T4 p3               ) { return ( api[name] as Func<T1, T2, T3, T4, T5        > )( p0, p1, p2, p3         ); }
        public T6 Func<T1, T2, T3, T4, T5, T6    >( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4        ) { return ( api[name] as Func<T1, T2, T3, T4, T5, T6    > )( p0, p1, p2, p3, p4     ); }
        public T7 Func<T1, T2, T3, T4, T5, T6, T7>( string name, T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5 ) { return ( api[name] as Func<T1, T2, T3, T4, T5, T6, T7> )( p0, p1, p2, p3, p4, p5 ); }

        // - private methods -------------------------------------------------------------

        public MessagePackObject GetApiInfo() {
            return neovimIO.Request( "vim_get_api_info", new object[] {} )[1];
        }

        private void CreateApi() {
            api.Clear();
            var funcs = GetApiInfo().AsList()[1].AsDictionary()["functions"].AsList();

            foreach( var f in funcs ) {
                var func = f.AsDictionary();

                var parameters = func["parameters"].AsList();
                var async      = ( func.ContainsKey( "async" )    ) ? func["async"].AsBoolean() : false;
                var canFail    = ( func.ContainsKey( "can_fail" ) ) ? func["can_fail"].AsBoolean() : false;
                var returnType = func["return_type"].AsString();
                var name       = func["name"].AsString();

                var param = new List<string>();

                foreach( var p in parameters ) {
                    var pType = p.AsList()[0].AsString();
                    var pName = p.AsList()[1].AsString();
                    param.Add( pType );
                }

                var expr = CreateExpression( name, returnType, param, async, canFail );
                api.Add( name, expr );
            }

            // ui_* api is not included in list by vim_get_api_list
            var ui_attach     = CreateAction<long, long, bool>( "ui_attach"    , "void", new List<string>() { "Integer", "Integer", "Boolean" }, false, false );
            var ui_detach     = CreateAction                  ( "ui_detach"    , "void", new List<string>(), false, false );
            var ui_try_resize = CreateAction<long, long      >( "ui_try_resize", "void", new List<string>() { "Integer", "Integer" }, false, false );
            api.Add( "ui_attach"    , ui_attach     );
            api.Add( "ui_detach"    , ui_detach     );
            api.Add( "ui_try_resize", ui_try_resize );
        }

        private object CreateExpression( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if ( IsActionType                                        ( param, returnType ) ) { return CreateAction                                        ( name, returnType, param, async, canFail ); }
            if ( IsActionType<long                                  >( param, returnType ) ) { return CreateAction<long                                  >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, long                            >( param, returnType ) ) { return CreateAction<long, long                            >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, long, string                    >( param, returnType ) ) { return CreateAction<long, long, string                    >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, long, string[]                  >( param, returnType ) ) { return CreateAction<long, long, string[]                  >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, long, long, bool, bool, string[]>( param, returnType ) ) { return CreateAction<long, long, long, bool, bool, string[]>( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, long[]                          >( param, returnType ) ) { return CreateAction<long, long[]                          >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, string                          >( param, returnType ) ) { return CreateAction<long, string                          >( name, returnType, param, async, canFail ); }
            if ( IsActionType<long, string, object                  >( param, returnType ) ) { return CreateAction<long, string, object                  >( name, returnType, param, async, canFail ); }
            if ( IsActionType<string                                >( param, returnType ) ) { return CreateAction<string                                >( name, returnType, param, async, canFail ); }
            if ( IsActionType<string, string, bool                  >( param, returnType ) ) { return CreateAction<string, string, bool                  >( name, returnType, param, async, canFail ); }
            if ( IsActionType<string, object                        >( param, returnType ) ) { return CreateAction<string, object                        >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long                                  >( param, returnType ) ) { return CreateFunc  <long                                  >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, bool                            >( param, returnType ) ) { return CreateFunc  <long, bool                            >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, long                            >( param, returnType ) ) { return CreateFunc  <long, long                            >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, long[]                          >( param, returnType ) ) { return CreateFunc  <long, long[]                          >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, long, string                    >( param, returnType ) ) { return CreateFunc  <long, long, string                    >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, long, long, bool, bool, string[]>( param, returnType ) ) { return CreateFunc  <long, long, long, bool, bool, string[]>( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, string                          >( param, returnType ) ) { return CreateFunc  <long, string                          >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, string, long[]                  >( param, returnType ) ) { return CreateFunc  <long, string, long[]                  >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, string, object                  >( param, returnType ) ) { return CreateFunc  <long, string, object                  >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long, string, object, object          >( param, returnType ) ) { return CreateFunc  <long, string, object, object          >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <long[]                                >( param, returnType ) ) { return CreateFunc  <long[]                                >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string                                >( param, returnType ) ) { return CreateFunc  <string                                >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, long                          >( param, returnType ) ) { return CreateFunc  <string, long                          >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, string                        >( param, returnType ) ) { return CreateFunc  <string, string                        >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, object                        >( param, returnType ) ) { return CreateFunc  <string, object                        >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, object, object                >( param, returnType ) ) { return CreateFunc  <string, object, object                >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, bool, bool, bool, string      >( param, returnType ) ) { return CreateFunc  <string, bool, bool, bool, string      >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string, MessagePackObject, object     >( param, returnType ) ) { return CreateFunc  <string, MessagePackObject, object     >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <string[]                              >( param, returnType ) ) { return CreateFunc  <string[]                              >( name, returnType, param, async, canFail ); }
            if ( IsFuncType  <MessagePackObject                     >( param, returnType ) ) { return CreateFunc  <MessagePackObject                     >( name, returnType, param, async, canFail ); }
            throw new NotImplementedException();
        }

        private bool IsFuncType<T1>( List<string> p, string r ) {
            return p.Count == 0 && GetType( r ) == typeof( T1 );
        }

        private bool IsFuncType<T1, T2>( List<string> p, string r ) {
            return p.Count == 1 && GetType( p[0] ) == typeof( T1 ) && GetType( r ) == typeof( T2 );
        }

        private bool IsFuncType<T1, T2, T3>( List<string> p, string r ) {
            return p.Count == 2 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( r ) == typeof( T3 );
        }

        private bool IsFuncType<T1, T2, T3, T4>( List<string> p, string r ) {
            return p.Count == 3 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( r ) == typeof( T4 );
        }

        private bool IsFuncType<T1, T2, T3, T4, T5>( List<string> p, string r ) {
            return p.Count == 4 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( r ) == typeof( T5 );
        }

        private bool IsFuncType<T1, T2, T3, T4, T5, T6>( List<string> p, string r ) {
            return p.Count == 5 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( p[4] ) == typeof( T5 ) && GetType( r ) == typeof( T6 );
        }

        private bool IsFuncType<T1, T2, T3, T4, T5, T6, T7>( List<string> p, string r ) {
            return p.Count == 6 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( p[4] ) == typeof( T5 ) && GetType( p[5] ) == typeof( T6 ) && GetType( r ) == typeof( T7 );
        }

        private bool IsActionType( List<string> p, string r ) {
            return p.Count == 0 && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1>( List<string> p, string r ) {
            return p.Count == 1 && GetType( p[0] ) == typeof( T1 ) && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1, T2>( List<string> p, string r ) {
            return p.Count == 2 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1, T2, T3>( List<string> p, string r ) {
            return p.Count == 3 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1, T2, T3, T4>( List<string> p, string r ) {
            return p.Count == 4 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1, T2, T3, T4, T5>( List<string> p, string r ) {
            return p.Count == 5 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( p[4] ) == typeof( T5 ) && GetType( r ) == typeof( void );
        }

        private bool IsActionType<T1, T2, T3, T4, T5, T6>( List<string> p, string r ) {
            return p.Count == 6 && GetType( p[0] ) == typeof( T1 ) && GetType( p[1] ) == typeof( T2 ) && GetType( p[2] ) == typeof( T3 ) && GetType( p[3] ) == typeof( T4 ) && GetType( p[4] ) == typeof( T5 ) && GetType( p[5] ) == typeof( T6 ) && GetType( r ) == typeof( void );
        }

        private object CreateFunc<T1>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T1 ) == typeof( long              ) ) { Expression<Func<long             >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T1 ) == typeof( long[]            ) ) { Expression<Func<long[]           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T1 ) == typeof( string            ) ) { Expression<Func<string           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T1 ) == typeof( string[]          ) ) { Expression<Func<string[]         >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T1 ) == typeof( bool              ) ) { Expression<Func<bool             >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T1 ) == typeof( object            ) ) { Expression<Func<object           >> func = ( () => neovimIO.Request( name, new object[] {}, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T1 ) == typeof( MessagePackObject ) ) { Expression<Func<MessagePackObject>> func = ( () => neovimIO.Request( name, new object[] {}, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T2 ) == typeof( long              ) ) { Expression<Func<T1, long             >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T2 ) == typeof( long[]            ) ) { Expression<Func<T1, long[]           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T2 ) == typeof( string            ) ) { Expression<Func<T1, string           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T2 ) == typeof( string[]          ) ) { Expression<Func<T1, string[]         >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T2 ) == typeof( bool              ) ) { Expression<Func<T1, bool             >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T2 ) == typeof( object            ) ) { Expression<Func<T1, object           >> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T2 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, MessagePackObject>> func = ( p => neovimIO.Request( name, new object[] { p }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T3 ) == typeof( long              ) ) { Expression<Func<T1, T2, long             >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T3 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, long[]           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T3 ) == typeof( string            ) ) { Expression<Func<T1, T2, string           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T3 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, string[]         >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T3 ) == typeof( bool              ) ) { Expression<Func<T1, T2, bool             >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T3 ) == typeof( object            ) ) { Expression<Func<T1, T2, object           >> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T3 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, MessagePackObject>> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T4 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, long             >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T4 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, long[]           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T4 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, string           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T4 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, string[]         >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T4 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, bool             >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T4 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, object           >> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T4 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, MessagePackObject>> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T5 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, long             >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T5 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, long[]           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T5 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, string           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T5 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, string[]         >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T5 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, bool             >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T5 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, object           >> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T5 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, MessagePackObject>> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5, T6>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T6 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, T5, long             >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsInt64orExt() );                                     return func.Compile(); }
            if( typeof( T6 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, T5, long[]           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsList().Select( i => i.AsInt64orExt() ).ToArray() ); return func.Compile(); }
            if( typeof( T6 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, T5, string           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsString() );                                         return func.Compile(); }
            if( typeof( T6 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, T5, string[]         >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() );     return func.Compile(); }
            if( typeof( T6 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, T5, bool             >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].AsBoolean() );                                        return func.Compile(); }
            if( typeof( T6 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, T5, object           >> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1].ToObject() );                                         return func.Compile(); }
            if( typeof( T6 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, T5, MessagePackObject>> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true )[1] );                                                    return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateFunc<T1, T2, T3, T4, T5, T6, T7>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            if( typeof( T7 ) == typeof( long              ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, long             >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsInt64() );                                      return func.Compile(); }
            if( typeof( T7 ) == typeof( long[]            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, long[]           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsList().Select( i => i.AsInt64() ).ToArray() );  return func.Compile(); }
            if( typeof( T7 ) == typeof( string            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, string           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsString() );                                     return func.Compile(); }
            if( typeof( T7 ) == typeof( string[]          ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, string[]         >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsList().Select( i => i.AsString() ).ToArray() ); return func.Compile(); }
            if( typeof( T7 ) == typeof( bool              ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, bool             >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].AsBoolean() );                                    return func.Compile(); }
            if( typeof( T7 ) == typeof( object            ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, object           >> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1].ToObject() );                                     return func.Compile(); }
            if( typeof( T7 ) == typeof( MessagePackObject ) ) { Expression<Func<T1, T2, T3, T4, T5, T6, MessagePackObject>> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true )[1] );                                                return func.Compile(); }
            throw new NotImplementedException();
        }

        private object CreateAction( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action> func = ( () => neovimIO.Request( name, new object[] {}, true ) );
            return func.Compile();
        }

        private object CreateAction<T1>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1>> func = ( p => neovimIO.Request( name, new object[] { p }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2>> func = ( ( p0, p1 ) => neovimIO.Request( name, new object[] { p0, p1 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3>> func = ( ( p0, p1, p2 ) => neovimIO.Request( name, new object[] { p0, p1, p2 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4>> func = ( ( p0, p1, p2, p3 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5>> func = ( ( p0, p1, p2, p3, p4 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5, T6>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5, T6>> func = ( ( p0, p1, p2, p3, p4, p5 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5 }, true ) );
            return func.Compile();
        }

        private object CreateAction<T1, T2, T3, T4, T5, T6, T7>( string name, string returnType, List<string> param, bool async, bool canFail ) {
            Expression<Action<T1, T2, T3, T4, T5, T6, T7>> func = ( ( p0, p1, p2, p3, p4, p5, p6 ) => neovimIO.Request( name, new object[] { p0, p1, p2, p3, p4, p5, p6 }, true ) );
            return func.Compile();
        }

        private Type GetType( string type ) {
            if ( type == "Buffer" || type == "Window" || type == "Tabpage" ) {
                return typeof( long );
            } else if ( type.StartsWith( "ArrayOf(Integer" ) ) {
                return typeof( long[] );
            } else if ( type.StartsWith( "ArrayOf(Buffer" ) || type.StartsWith( "ArrayOf(Window" ) || type.StartsWith( "ArrayOf(Tabpage" ) ) {
                return typeof( long[] );
            } else if ( type.StartsWith( "ArrayOf(String" ) ) {
                return typeof( string[] );
            } else {
                switch( type ) {
                    case "Integer"   : return typeof( long     );
                    case "String"    : return typeof( string   );
                    case "void"      : return typeof( void     );
                    case "Boolean"   : return typeof( bool     );
                    case "Object"    : return typeof( object   );
                    case "Array"     : return typeof( MessagePackObject );
                    case "Dictionary": return typeof( MessagePackObject );
                    default          : throw new NotImplementedException();
                }
            }
        }

    }
}
