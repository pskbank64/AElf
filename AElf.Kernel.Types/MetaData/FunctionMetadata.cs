using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    /// <summary>
    /// The Metadata will not be changed after they are calculated as long as the related contracts don't update.
    /// Thus for each function, we store the whole set of metadata (which generated by accessing function's metadata recursively according to calling_set)
    /// When the contracts update, the metadata of related contracts' functions must be updated accordingly.
    /// </summary>
    public partial class FunctionMetadata
    {
        public FunctionMetadata(HashSet<string> callingSet, HashSet<Resource> fullResourceSet)
        {
            SerializeCallingSet.AddRange(callingSet.AsEnumerable());
            SerializeFullResourceSet.AddRange(fullResourceSet.AsEnumerable());
        }

        /// <summary>
        /// used to find influenced contract when a contract is updated
        /// </summary>
        public HashSet<string> CallingSet => new HashSet<string>(SerializeCallingSet);

        /// <summary>
        /// used to find what resource this function will access (recursive)
        /// </summary>
        public HashSet<Resource> FullResourceSet => new HashSet<Resource>(SerializeFullResourceSet);

        bool IEquatable<FunctionMetadata>.Equals(FunctionMetadata other)
        {
            return HashSet<string>.CreateSetComparer().Equals(CallingSet, other.CallingSet) &&
                   HashSet<Resource>.CreateSetComparer().Equals(FullResourceSet, other.FullResourceSet);
        }
    }
}