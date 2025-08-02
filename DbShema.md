USE [KufeArtAppDB]
GO
/****** Object:  Table [dbo].[AddtionHistories]    Script Date: 2.08.2025 13:25:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AddtionHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[AddionStatusId] [uniqueidentifier] NOT NULL,
	[ShorLabel] [nvarchar](50) NULL,
	[ProductName] [nvarchar](50) NOT NULL,
	[ProductPrice] [float] NOT NULL,
	[ProductQuantity] [int] NOT NULL,
	[TotalPrice] [float] NOT NULL,
	[PersonId] [uniqueidentifier] NOT NULL,
	[PersonFullName] [nvarchar](50) NOT NULL,
	[OrderBatchId] [uniqueidentifier] NULL,
	[TableId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AddtionHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CampaignProgresses]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CampaignProgresses](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CustomerId] [uniqueidentifier] NOT NULL,
	[CampaignId] [uniqueidentifier] NOT NULL,
	[CurrentQuantity] [int] NOT NULL,
	[EarnedFreeQuantity] [int] NOT NULL,
	[LastPurchaseDate] [datetime] NOT NULL,
	[CampaignStartDate] [datetime] NOT NULL,
 CONSTRAINT [PK_CampaignProgresses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Campaigns]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Campaigns](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[RequiredQuantity] [int] NOT NULL,
	[FreeQuantity] [int] NOT NULL,
	[ValidityDays] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NULL,
 CONSTRAINT [PK_Campaigns] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerPoints]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerPoints](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CustomerId] [uniqueidentifier] NOT NULL,
	[TotalPoints] [int] NOT NULL,
 CONSTRAINT [PK_CustomerPoints] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CustomerPurchaseHistories]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CustomerPurchaseHistories](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CustomerId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[CampaignId] [uniqueidentifier] NULL,
	[Quantity] [int] NOT NULL,
	[UnitPrice] [float] NOT NULL,
	[TotalPrice] [float] NOT NULL,
	[IsFreeCampaignProduct] [bit] NOT NULL,
 CONSTRAINT [PK_CustomerPurchaseHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Customers]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customers](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Fullname] [nvarchar](50) NOT NULL,
	[PhoneNumber] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Password] [nvarchar](50) NULL,
 CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[KufePointTransactions]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KufePointTransactions](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CustomerId] [uniqueidentifier] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[Type] [int] NOT NULL,
	[Points] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_KufePointTransactions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderBatchStatuses]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderBatchStatuses](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[OrderBatchId] [uniqueidentifier] NOT NULL,
	[IsReady] [bit] NOT NULL,
	[CompletedBy] [nvarchar](50) NULL,
	[Department] [nvarchar](50) NULL,
	[CompletedAt] [datetime] NULL,
 CONSTRAINT [PK_OrderBatchStatuses] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payments]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payments](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[TableId] [uniqueidentifier] NOT NULL,
	[AddionStatusId] [uniqueidentifier] NOT NULL,
	[PaymentType] [int] NOT NULL,
	[Amount] [float] NOT NULL,
	[ShortLabel] [nvarchar](50) NULL,
	[PersonId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Persons]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Persons](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[FullName] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[AccessType] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[ProfileImagePath] [nvarchar](max) NULL,
	[IsSuperAdmin] [bit] NULL,
 CONSTRAINT [PK_Persons] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductImages]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductImages](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ProductId] [uniqueidentifier] NOT NULL,
	[ImagePath] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_ProductImages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[CategoryName] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Price] [float] NOT NULL,
	[IsQrMenu] [bit] NOT NULL,
	[IsTvMenu] [bit] NULL,
	[Type] [int] NOT NULL,
	[HasCampaign] [bit] NOT NULL,
	[CampaignCaption] [nvarchar](50) NULL,
	[CampaignDetail] [nvarchar](max) NULL,
	[IsActive] [bit] NULL,
	[IsCommissionEligible] [bit] NOT NULL,
	[CommissionRate] [float] NOT NULL,
	[HasKufePoints] [bit] NULL,
	[KufePoints] [int] NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QrMenuVisibles]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QrMenuVisibles](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[IpAdress] [nvarchar](50) NULL,
 CONSTRAINT [PK_QrMenuVisibles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tables]    Script Date: 2.08.2025 13:25:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tables](
	[Id] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[AddionStatus] [uniqueidentifier] NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Category] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Tables] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AddtionHistories] ADD  CONSTRAINT [DF_AddtionHistories_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[AddtionHistories] ADD  CONSTRAINT [DF_AddtionHistories_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[CampaignProgresses] ADD  CONSTRAINT [DF_CampaignProgresses_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CampaignProgresses] ADD  CONSTRAINT [DF_CampaignProgresses_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Campaigns] ADD  CONSTRAINT [DF_Campaigns_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Campaigns] ADD  CONSTRAINT [DF_Campaigns_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[CustomerPoints] ADD  CONSTRAINT [DF_CustomerPoints_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CustomerPoints] ADD  CONSTRAINT [DF_CustomerPoints_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[CustomerPurchaseHistories] ADD  CONSTRAINT [DF_CustomerPurchaseHistories_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[CustomerPurchaseHistories] ADD  CONSTRAINT [DF_CustomerPurchaseHistories_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Customers] ADD  CONSTRAINT [DF_Customers_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Customers] ADD  CONSTRAINT [DF_Customers_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[KufePointTransactions] ADD  CONSTRAINT [DF_KufePointTransactions_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[KufePointTransactions] ADD  CONSTRAINT [DF_KufePointTransactions_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[OrderBatchStatuses] ADD  CONSTRAINT [DF_OrderBatchStatuses_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[OrderBatchStatuses] ADD  CONSTRAINT [DF_OrderBatchStatuses_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Payments] ADD  CONSTRAINT [DF_Payments_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Persons] ADD  CONSTRAINT [DF_Persons_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Persons] ADD  CONSTRAINT [DF_Persons_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ProductImages] ADD  CONSTRAINT [DF_ProductImages_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[ProductImages] ADD  CONSTRAINT [DF_ProductImages_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Products] ADD  CONSTRAINT [DF_Products_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Products] ADD  CONSTRAINT [DF_Products_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[QrMenuVisibles] ADD  CONSTRAINT [DF_QrMenuVisibles_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[QrMenuVisibles] ADD  CONSTRAINT [DF_QrMenuVisibles_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Tables] ADD  CONSTRAINT [DF_Tables_Id]  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Tables] ADD  CONSTRAINT [DF_Tables_CreatedAt]  DEFAULT (getdate()) FOR [CreatedAt]
GO
