public sealed record FrontOrderItem(int id, int quantity);
public sealed record FrontCreateOrderRequest(List<FrontOrderItem> products);
