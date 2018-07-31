﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ModCore.Logic.EntityFramework.AttributeImpl;

namespace ModCore.Database
{
    [Table("mcore_cmd_state")]
    public class DatabaseCommandId
    {
        [AlternateKey("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)]
        [Annotation("Npgsql:ValueGeneratedOnAdd", true)]
        [Annotation("Sqlite:Autoincrement", true)]
        public short Id { get; set; }
        
        [Key]
        [Column("command_qualified")]
        public string Command { get; set; }
    }
}