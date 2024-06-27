using System.Collections.Generic;

namespace GameSharedInterfaces
{
    public struct UnitPath
    {
        public int iCadre;
        public int[] Path;

        public UnitPath(int iCadre, int[] path)
        {
            this.iCadre = iCadre;
            this.Path = path;
        }
    }

    public struct UnitMove
    {
        public int iCadre;
        public int iStart;
        public int iEnd;
        
        public UnitMove(int iCadre, int iStart, int iEnd)
        {
            this.iCadre = iCadre;
            this.iStart = iStart;
            this.iEnd = iEnd;
        }
    }
    
    public class UnitPathCollection
    {
        public List<UnitMove> UnitMoves;
        public UnitPath[] ValidPaths; // Not automatically calculated. Must be calculated by an outside method
    }
}