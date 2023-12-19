namespace Bluewire.Common.GitWrapper.Model
{
    public struct ObjectTypeParser
    {
        public bool TryParse(string value, out ObjectType type)
        {
            switch (value)
            {
                case "tree":
                    type = ObjectType.Tree;
                    return true;
                case "blob":
                    type = ObjectType.Blob;
                    return true;
            }
            type = ObjectType.None;
            return false;
        }
    }
}
