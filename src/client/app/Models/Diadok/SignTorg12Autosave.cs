using AnalitF.Net.Client.Config.NHibernate;
using Caliburn.Micro;
using System;
using System.Runtime.Serialization;

namespace AnalitF.Net.Client.Models
{
	public class SignTorg12Autosave
	{
		public SignTorg12Autosave()
		{
			CreationDate = DateTime.Now;
		}

		public virtual uint Id { get; set; }
		public virtual DateTime CreationDate { get; set; }
		public virtual string SignerJobTitle { get; set; }
		public virtual bool LikeReciever { get; set; }
		public virtual string ACPTSurename { get; set; }
		public virtual string ACPTFirstName { get; set; }
		public virtual string ACPTPatronimic { get; set; }
		public virtual string ACPTJobTitle { get; set; }
		public virtual bool ByAttorney { get; set; }
		public virtual string ATRNum { get; set; }
		public virtual DateTime ATRDate { get; set; }
		public virtual string ATROrganization { get; set; }
		public virtual string ATRSurename { get; set; }
		public virtual string ATRFirstName { get; set; }
		public virtual string ATRPatronymic { get; set; }
		public virtual string ATRAddInfo { get; set; }
		public virtual string Comment { get; set; }

		[IgnoreDataMember, Ignore]
		public virtual string DisplayName
		{
			get
			{
				if(LikeReciever)
					if(ByAttorney)
						return $"{SignerJobTitle}/Совпд. с получ./По довер./{ATRNum}/{ATRDate.ToString("dd.MM.yyyy")}/"+
							$"{ATROrganization}/{ATRSurename}/{ATRFirstName}/{ATRPatronymic}/{ATRAddInfo}/{Comment}";
					else
						return $"{SignerJobTitle}/Совпд. с получ./"+
							$"{Comment}";
				else
					if(ByAttorney)
						return $"{SignerJobTitle}/{ACPTSurename}/{ACPTFirstName}/{ACPTPatronimic}/По довер./{ATRNum}/{ATRDate.ToString("dd.MM.yyyy")}/"+
							$"{ATROrganization}/{ATRSurename}/{ATRFirstName}/{ATRPatronymic}/{ATRAddInfo}/{Comment}";
					else
						return $"{SignerJobTitle}/{ACPTSurename}/{ACPTFirstName}/{ACPTPatronimic}/"+
							$"{Comment}";
			}
		}
	}
}