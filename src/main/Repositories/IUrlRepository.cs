using Model;

namespace IUrlRepository
{
    public interface IUrlRepo
    {
        void Add(ModelUrl modelurl);
        void Save();
    }
}