﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using StamAcasa.Common.DTO;
using StamAcasa.Common.Models;

namespace StamAcasa.Common.Services {
    public class UserService : IUserService {
        private readonly UserDbContext _context;
        private readonly IMapper _mapper;

        public UserService(UserDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private async Task<bool> AddOrUpdateEntity(User user, UserModel userUpdateInfo)
        {
            if (user == null) {
                var newUserInfo = _mapper.Map<User>(userUpdateInfo);
                var saved = await _context.Users.AddAsync(newUserInfo);
                await _context.SaveChangesAsync();
                return saved.Entity.Id > 0;
            }

            foreach (var prop in typeof(UserModel).GetProperties().Where(p=>p.Name!="Id"))
                user
                    .GetType()
                    .GetProperty(prop.Name)
                    ?.SetValue(user, prop.GetValue(userUpdateInfo));
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> AddOrUpdateUserInfo(UserModel userUpdateInfo)
        {
            var user = _context.Users.FirstOrDefault(u => u.Sub == userUpdateInfo.Sub);
            return await AddOrUpdateEntity(user, userUpdateInfo);
        }

        public async Task<bool> AddOrUpdateDependentInfo(UserModel dependentInfo, string parentSub)
        {
            var parentUser = _context.Users.FirstOrDefault(u => u.Sub == parentSub);
            if (parentUser == null)
                return false;

            dependentInfo.ParentId = parentUser.Id;
            User existingUserEntity = null;
            if(dependentInfo.Id.HasValue) 
                existingUserEntity = _context.Users.FirstOrDefault(u => u.Id == dependentInfo.Id.Value);

            return await AddOrUpdateEntity(existingUserEntity, dependentInfo);
        }

        public async Task<UserInfo> GetUserInfo(string sub)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u=>u.Sub == sub);
            var result = _mapper.Map<UserInfo>(user);
            return result;
        }
        public async Task<UserInfo> GetUserInfo(int id) {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            var result = _mapper.Map<UserInfo>(user);
            return result;
        }

        public async Task<IEnumerable<UserInfo>> GetDependentInfo(string sub)
        {
            var user = await _context.Users
                .Include("DependentUsers")
                .FirstOrDefaultAsync(u => u.Sub == sub);
            var result = user.DependentUsers.Select(d=>_mapper.Map<UserInfo>(d));
            return result;
        }
    }
}
