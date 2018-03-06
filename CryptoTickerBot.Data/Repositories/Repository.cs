﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CryptoTickerBot.Data.Persistence;
using JetBrains.Annotations;

namespace CryptoTickerBot.Data.Repositories
{
	public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
	{
		protected readonly CtbContext Context;

		protected virtual IQueryable<TEntity> AllEntities { get; }

		public Repository ( [NotNull] CtbContext context )
		{
			Context     = context;
			AllEntities = Context.Set<TEntity> ( );
		}

		public virtual TEntity Get ( params object[] id ) =>
			Context.Set<TEntity> ( ).Find ( id );

		public virtual IEnumerable<TEntity> GetAll ( ) =>
			AllEntities.ToList ( );

		public virtual IEnumerable<TEntity> Find ( Expression<Func<TEntity, bool>> predicate ) =>
			AllEntities.Where ( predicate );

		public virtual async Task<TEntity> GetAsync ( object id, CancellationToken cancellationToken ) =>
			await Context.Set<TEntity> ( ).FindAsync ( cancellationToken, id );

		public virtual async Task<IEnumerable<TEntity>> GetAllAsync ( CancellationToken cancellationToken ) =>
			await AllEntities.ToListAsync ( cancellationToken );

		public virtual async Task<IEnumerable<TEntity>> FindAsync (
			Expression<Func<TEntity, bool>> predicate,
			CancellationToken cancellationToken ) =>
			await AllEntities.Where ( predicate ).ToListAsync ( cancellationToken );

		public void Add ( TEntity entity ) =>
			Context.Set<TEntity> ( ).Add ( entity );

		public void AddRange ( IEnumerable<TEntity> entities ) =>
			Context.Set<TEntity> ( ).AddRange ( entities );

		public void Remove ( TEntity entity ) =>
			Context.Set<TEntity> ( ).Remove ( entity );

		public void Remove ( Expression<Func<TEntity, bool>> predicate ) =>
			RemoveRange ( Context.Set<TEntity> ( ).Where ( predicate ) );

		public void RemoveRange ( IEnumerable<TEntity> entities ) =>
			Context.Set<TEntity> ( ).RemoveRange ( entities );
	}
}