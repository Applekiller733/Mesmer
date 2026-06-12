using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using SongAppApi.Helpers;
using SongAppApi.Helpers.Resolvers;

namespace SongAppApi.Tests.Common
{
    public static class TestMapperFactory
    {
        public static IMapper Create(DataContext context)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(context);
            services.AddTransient<CreatePlaylistSongResolver>();
            services.AddTransient<UpdatePlaylistSongResolver>();
            services.AddTransient<SoundUrlResolver>();
            services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile).Assembly);
            var sp = services.BuildServiceProvider();
            return sp.GetRequiredService<IMapper>();
        }
    }
}
