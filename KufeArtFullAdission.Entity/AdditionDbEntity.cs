using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Entity;

public sealed class AdditionDbEntity:BaseDbEntity
{
    public Guid? Status { get; set; }
    public Guid TableId { get; set; }
}
