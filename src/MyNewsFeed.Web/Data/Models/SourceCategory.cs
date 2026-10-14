using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyNewsFeed.Web.Data.Models;

[Index("CategoryName", Name = "UQ_SourceCategories_Category_Name", IsUnique = true)]
public partial class SourceCategory
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("Category_Name")]
    [StringLength(100)]
    public string CategoryName { get; set; } = null!;

    public DateTime DateAdded { get; set; }

    [InverseProperty("SourceCategory")]
    public virtual ICollection<Source> Sources { get; set; } = new List<Source>();
}
