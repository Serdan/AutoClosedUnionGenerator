namespace Kehlet.Functional
{
    public class AutoClosedAttribute : Attribute
    { 
        public AutoClosedAttribute(bool serializable = false) { }
    }
}
